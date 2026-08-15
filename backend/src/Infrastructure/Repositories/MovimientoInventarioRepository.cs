using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Common;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class MovimientoInventarioRepository : IMovimientoInventarioRepository
{
    private readonly AppDbContext _context;
    private readonly IUsuarioScopeService _usuarioScope;

    public MovimientoInventarioRepository(AppDbContext context, IUsuarioScopeService usuarioScope)
    {
        _context = context;
        _usuarioScope = usuarioScope;
    }

    private static IQueryable<MovimientoInventario> AplicarAlcance(IQueryable<MovimientoInventario> query, UsuarioScopeActual? alcance)
    {
        if (alcance is null) return query.Where(_ => false);
        return alcance.EsAdministrador ? query : query.Where(m => m.CreadoPorUsuarioId == alcance.UsuarioId);
    }

    private IQueryable<MovimientoInventario> ConIncludes() =>
        _context.MovimientosInventario
            .Include(m => m.Producto)
                .ThenInclude(p => p!.Imagenes)
            .Include(m => m.ProductoVariante)
                .ThenInclude(v => v!.Color)
            .AsSplitQuery();

    public async Task AddAsync(MovimientoInventario movimiento) =>
        await _context.MovimientosInventario.AddAsync(movimiento);

    public async Task AddConOrigenTipadoAsync(
        MovimientoInventario movimiento,
        OrigenMovimientoInventario origen)
    {
        ArgumentNullException.ThrowIfNull(movimiento);
        ArgumentNullException.ThrowIfNull(origen);

        movimiento.ReferenciaTipo = CrearReferenciaTipoSnapshot(movimiento, origen);
        movimiento.ReferenciaId = origen.DocumentoId;
        movimiento.CompraId = origen.CompraId;
        movimiento.VentaId = origen.VentaId;
        movimiento.ConsumoInsumoId = origen.ConsumoInsumoId;
        movimiento.AjusteInventarioId = origen.AjusteInventarioId;

        // Compatibilidad de corte N1.5: los productores legacy que todavía no
        // entregan un CorrelationId explícito quedan agrupados por operación y
        // fase empresarial, sin inventar contexto físico. Los callers ya
        // migrados conservan su correlación explícita, pero también pasan por
        // la misma validación fail-closed del writer canónico.
        movimiento.CorrelationId = string.IsNullOrWhiteSpace(movimiento.CorrelationId)
            ? CrearCorrelationIdCompatibilidad(movimiento.ReferenciaTipo, origen.DocumentoId)
            : NormalizarCorrelationId(movimiento.CorrelationId);

        await AddAsync(movimiento);
    }

    private static string CrearReferenciaTipoSnapshot(
        MovimientoInventario movimiento,
        OrigenMovimientoInventario origen) =>
        origen.Tipo switch
        {
            TipoOrigenMovimientoInventario.Compra when movimiento.Causa == CausaMovimientoInventario.AnulacionCompra => "CompraAnulada",
            TipoOrigenMovimientoInventario.Compra => "Compra",
            TipoOrigenMovimientoInventario.Venta when movimiento.Causa == CausaMovimientoInventario.AnulacionVenta => "VentaAnulada",
            TipoOrigenMovimientoInventario.Venta => "Venta",
            TipoOrigenMovimientoInventario.ConsumoInsumo => "ConsumoInsumo",
            TipoOrigenMovimientoInventario.AjusteInventario => "AjusteInventario",
            _ => throw new InvalidOperationException($"Origen de inventario no soportado: {origen.Tipo}.")
        };

    private static string CrearCorrelationIdCompatibilidad(string referenciaTipo, int documentoId)
    {
        if (documentoId <= 0)
            throw new InvalidOperationException("El origen tipado debe estar persistido antes de registrar movimientos de Kardex.");

        return NormalizarCorrelationId($"{referenciaTipo.ToLowerInvariant()}:{documentoId}");
    }

    private static string NormalizarCorrelationId(string correlationId)
    {
        var normalizado = correlationId.Trim();
        if (normalizado.Length == 0)
            throw new InvalidOperationException("CorrelationId no puede ser vacío en un movimiento nuevo de Kardex.");
        if (normalizado.Length > ContextoFisicoMovimientoInventario.MaxCorrelationIdLength)
        {
            throw new InvalidOperationException(
                $"CorrelationId excede {ContextoFisicoMovimientoInventario.MaxCorrelationIdLength} caracteres.");
        }
        if (!normalizado.All(EsCaracterSeguroCorrelationId))
            throw new InvalidOperationException("CorrelationId contiene caracteres no permitidos.");

        return normalizado;
    }

    private static bool EsCaracterSeguroCorrelationId(char value) =>
        char.IsAsciiLetterOrDigit(value) || value is '-' or '_' or '.' or ':';

    public async Task<List<MovimientoInventario>> GetByProductoAsync(int productoId)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        return await AplicarAlcance(ConIncludes(), alcance)
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();
    }

    public async Task<List<MovimientoInventario>> GetFilteredAsync(int? productoId, string? tipo, DateTime? desde, DateTime? hasta)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var query = AplicarAlcance(ConIncludes(), alcance);
        if (productoId.HasValue) query = query.Where(m => m.ProductoId == productoId.Value);

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            if (Enum.TryParse<TipoMovimientoInventario>(tipo.Trim(), ignoreCase: true, out var tipoMovimiento) &&
                Enum.IsDefined(tipoMovimiento))
            {
                query = query.Where(m => m.Tipo == tipoMovimiento);
            }
            else
            {
                query = query.Where(_ => false);
            }
        }

        if (desde.HasValue) query = query.Where(m => m.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(m => m.Fecha <= hasta.Value);
        return await query.OrderByDescending(m => m.Fecha).Take(200).ToListAsync();
    }

    public async Task<IReadOnlyDictionary<int, MovimientoInventarioOrigenPersistido>> GetOrigenesTipadosAsync(
        IReadOnlyCollection<int> movimientoIds)
    {
        var ids = movimientoIds.Distinct().OrderBy(x => x).ToArray();
        if (ids.Length == 0)
            return new Dictionary<int, MovimientoInventarioOrigenPersistido>();

        if (!_context.Database.IsRelational())
        {
            var legacy = await _context.MovimientosInventario
                .AsNoTracking()
                .Where(m => ids.Contains(m.Id))
                .Select(m => new { m.Id, m.ReferenciaTipo, m.ReferenciaId })
                .ToListAsync();

            return legacy.ToDictionary(
                m => m.Id,
                m => CrearOrigenCompatibilidadNoRelacional(m.Id, m.ReferenciaTipo, m.ReferenciaId));
        }

        var movimientos = await _context.MovimientosInventario
            .AsNoTracking()
            .Where(m => ids.Contains(m.Id))
            .Select(m => new
            {
                m.Id,
                m.CompraId,
                m.VentaId,
                m.ConsumoInsumoId,
                m.AjusteInventarioId
            })
            .ToListAsync();

        var resultado = new Dictionary<int, MovimientoInventarioOrigenPersistido>(movimientos.Count);
        foreach (var movimiento in movimientos)
        {
            var cantidadOrigenes =
                (movimiento.CompraId.HasValue ? 1 : 0) +
                (movimiento.VentaId.HasValue ? 1 : 0) +
                (movimiento.ConsumoInsumoId.HasValue ? 1 : 0) +
                (movimiento.AjusteInventarioId.HasValue ? 1 : 0);

            if (cantidadOrigenes > 1)
                throw new InvalidOperationException($"El movimiento {movimiento.Id} tiene más de un origen tipado persistido.");

            resultado[movimiento.Id] = new MovimientoInventarioOrigenPersistido(
                movimiento.Id,
                movimiento.CompraId,
                movimiento.VentaId,
                movimiento.ConsumoInsumoId,
                movimiento.AjusteInventarioId);
        }

        return resultado;
    }

    private static MovimientoInventarioOrigenPersistido CrearOrigenCompatibilidadNoRelacional(
        int movimientoId,
        string referenciaTipo,
        int referenciaId) =>
        referenciaTipo switch
        {
            "Compra" or "CompraAnulada" => new(movimientoId, referenciaId, null, null),
            "Venta" or "VentaAnulada" => new(movimientoId, null, referenciaId, null),
            "ConsumoInsumo" => new(movimientoId, null, null, referenciaId),
            "AjusteInventario" => new(movimientoId, null, null, null, referenciaId),
            _ => new(movimientoId, null, null, null)
        };

    public async Task<int?> GetUltimoMovimientoOriginalCompraIdAsync(int compraId)
    {
        if (!_context.Database.IsRelational())
        {
            return await _context.MovimientosInventario
                .AsNoTracking()
                .Where(m =>
                    m.ReferenciaTipo == "Compra" &&
                    m.ReferenciaId == compraId &&
                    m.Tipo == TipoMovimientoInventario.Entrada)
                .MaxAsync(m => (int?)m.Id);
        }

        return await _context.MovimientosInventario
            .AsNoTracking()
            .Where(m => m.CompraId == compraId && m.Tipo == TipoMovimientoInventario.Entrada)
            .MaxAsync(m => (int?)m.Id);
    }

    public async Task<bool> ExisteMovimientoPosteriorAsync(
        int ultimoMovimientoOriginalId,
        IReadOnlyCollection<int> productoIds)
    {
        var ids = productoIds.Distinct().OrderBy(x => x).ToArray();
        if (ids.Length == 0) return false;

        if (!_context.Database.IsRelational())
            return await ExisteMovimientoPosteriorLegacyParaProviderNoRelacionalAsync(ultimoMovimientoOriginalId, ids);

        var compraId = await _context.MovimientosInventario
            .AsNoTracking()
            .Where(m =>
                m.Id == ultimoMovimientoOriginalId &&
                m.CompraId != null &&
                m.Tipo == TipoMovimientoInventario.Entrada)
            .Select(m => m.CompraId)
            .SingleOrDefaultAsync();

        if (!compraId.HasValue)
            throw new InvalidOperationException(
                "El movimiento limite no corresponde a un movimiento original de compra tipado.");

        var clavesOriginales = await _context.MovimientosInventario
            .AsNoTracking()
            .Where(m =>
                m.CompraId == compraId.Value &&
                m.Tipo == TipoMovimientoInventario.Entrada &&
                ids.Contains(m.ProductoId))
            .Select(m => new { m.ProductoId, m.ProductoVarianteId })
            .Distinct()
            .OrderBy(x => x.ProductoId)
            .ThenBy(x => x.ProductoVarianteId)
            .ToListAsync();

        foreach (var clave in clavesOriginales)
        {
            if (await _context.MovimientosInventario
                .AsNoTracking()
                .AnyAsync(m =>
                    m.Id > ultimoMovimientoOriginalId &&
                    m.ProductoId == clave.ProductoId &&
                    m.ProductoVarianteId == clave.ProductoVarianteId))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> ExisteMovimientoPosteriorLegacyParaProviderNoRelacionalAsync(
        int ultimoMovimientoOriginalId,
        IReadOnlyCollection<int> productoIds)
    {
        var movimientoTope = await _context.MovimientosInventario
            .AsNoTracking()
            .Where(m =>
                m.Id == ultimoMovimientoOriginalId &&
                m.ReferenciaTipo == "Compra" &&
                m.Tipo == TipoMovimientoInventario.Entrada)
            .Select(m => new { m.ReferenciaId })
            .SingleOrDefaultAsync();

        if (movimientoTope is null)
            throw new InvalidOperationException(
                "El movimiento limite no corresponde a un movimiento original de compra.");

        var clavesOriginales = await _context.MovimientosInventario
            .AsNoTracking()
            .Where(m =>
                m.ReferenciaTipo == "Compra" &&
                m.ReferenciaId == movimientoTope.ReferenciaId &&
                productoIds.Contains(m.ProductoId))
            .Select(m => new { m.ProductoId, m.ProductoVarianteId })
            .Distinct()
            .OrderBy(x => x.ProductoId)
            .ThenBy(x => x.ProductoVarianteId)
            .ToListAsync();

        foreach (var clave in clavesOriginales)
        {
            if (await _context.MovimientosInventario
                .AsNoTracking()
                .AnyAsync(m =>
                    m.Id > ultimoMovimientoOriginalId &&
                    m.ProductoId == clave.ProductoId &&
                    m.ProductoVarianteId == clave.ProductoVarianteId))
            {
                return true;
            }
        }

        return false;
    }
}
