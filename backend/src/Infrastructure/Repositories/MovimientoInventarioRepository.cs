using System.Data;
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

        if (!_context.Database.IsRelational())
        {
            await AddAsync(movimiento);
            return;
        }

        var tipo = movimiento.Tipo.ToString();
        var causa = (int)movimiento.Causa;

        await _context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO MovimientosInventario
                (ProductoId, ProductoVarianteId,
                 ProductoMarcaSnapshot, ProductoModeloSnapshot, ProductoColorSnapshot,
                 ProductoTallaSnapshot, ProductoSkuSnapshot,
                 Tipo, Causa, Cantidad, StockAnterior, StockNuevo,
                 CostoUnitario, PrecioUnitario,
                 ReferenciaTipo, ReferenciaId,
                 CompraId, VentaId, ConsumoInsumoId,
                 Descripcion, CreadoPorUsuarioId, CreadoPorNombreUsuario, Fecha)
            VALUES
                ({movimiento.ProductoId}, {movimiento.ProductoVarianteId},
                 {movimiento.ProductoMarcaSnapshot}, {movimiento.ProductoModeloSnapshot}, {movimiento.ProductoColorSnapshot},
                 {movimiento.ProductoTallaSnapshot}, {movimiento.ProductoSkuSnapshot},
                 {tipo}, {causa}, {movimiento.Cantidad}, {movimiento.StockAnterior}, {movimiento.StockNuevo},
                 {movimiento.CostoUnitario}, {movimiento.PrecioUnitario},
                 {movimiento.ReferenciaTipo}, {movimiento.ReferenciaId},
                 {origen.CompraId}, {origen.VentaId}, {origen.ConsumoInsumoId},
                 {movimiento.Descripcion}, {movimiento.CreadoPorUsuarioId}, {movimiento.CreadoPorNombreUsuario}, {movimiento.Fecha})
            """);
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
            _ => throw new InvalidOperationException($"Origen de inventario no soportado: {origen.Tipo}.")
        };

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
            var movimientos = await _context.MovimientosInventario
                .AsNoTracking()
                .Where(m => ids.Contains(m.Id))
                .Select(m => new { m.Id, m.ReferenciaTipo, m.ReferenciaId })
                .ToListAsync();

            return movimientos.ToDictionary(
                m => m.Id,
                m => CrearOrigenCompatibilidadNoRelacional(m.Id, m.ReferenciaTipo, m.ReferenciaId));
        }

        var resultado = new Dictionary<int, MovimientoInventarioOrigenPersistido>();
        var connection = _context.Database.GetDbConnection();
        var abrirAqui = connection.State != ConnectionState.Open;
        if (abrirAqui)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            var parametros = new List<string>(ids.Length);
            for (var i = 0; i < ids.Length; i++)
            {
                var nombre = $"@id{i}";
                parametros.Add(nombre);
                var parameter = command.CreateParameter();
                parameter.ParameterName = nombre;
                parameter.Value = ids[i];
                command.Parameters.Add(parameter);
            }

            command.CommandText = $"""
                SELECT Id, CompraId, VentaId, ConsumoInsumoId
                  FROM MovimientosInventario
                 WHERE Id IN ({string.Join(", ", parametros)})
                """;

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                int? compraId = reader.IsDBNull(1) ? null : reader.GetInt32(1);
                int? ventaId = reader.IsDBNull(2) ? null : reader.GetInt32(2);
                int? consumoInsumoId = reader.IsDBNull(3) ? null : reader.GetInt32(3);
                var cantidadOrigenes = (compraId.HasValue ? 1 : 0) +
                                       (ventaId.HasValue ? 1 : 0) +
                                       (consumoInsumoId.HasValue ? 1 : 0);
                if (cantidadOrigenes > 1)
                    throw new InvalidOperationException($"El movimiento {id} tiene más de un origen tipado persistido.");

                resultado[id] = new MovimientoInventarioOrigenPersistido(
                    id, compraId, ventaId, consumoInsumoId);
            }
        }
        finally
        {
            if (abrirAqui)
                await connection.CloseAsync();
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
            .FromSqlInterpolated($"SELECT * FROM MovimientosInventario WHERE CompraId = {compraId}")
            .AsNoTracking()
            .Where(m => m.Tipo == TipoMovimientoInventario.Entrada)
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

        var movimientoTopeEsCompra = await _context.MovimientosInventario
            .FromSqlInterpolated($"SELECT * FROM MovimientosInventario WHERE Id = {ultimoMovimientoOriginalId} AND CompraId IS NOT NULL")
            .AsNoTracking()
            .Where(m => m.Tipo == TipoMovimientoInventario.Entrada)
            .AnyAsync();

        if (!movimientoTopeEsCompra)
            throw new InvalidOperationException(
                "El movimiento limite no corresponde a un movimiento original de compra tipado.");

        var clavesOriginales = await _context.MovimientosInventario
            .FromSqlInterpolated($"""
                SELECT m.*
                  FROM MovimientosInventario m
                  JOIN MovimientosInventario limite ON limite.Id = {ultimoMovimientoOriginalId}
                 WHERE limite.CompraId IS NOT NULL
                   AND limite.Tipo = 'Entrada'
                   AND m.CompraId = limite.CompraId
                   AND m.Tipo = 'Entrada'
                """)
            .AsNoTracking()
            .Where(m => ids.Contains(m.ProductoId))
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
