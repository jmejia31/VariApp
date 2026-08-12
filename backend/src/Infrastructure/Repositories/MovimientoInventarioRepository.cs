using InventoryApp.Application.Interfaces;
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

    public async Task<int?> GetUltimoMovimientoOriginalCompraIdAsync(int compraId)
    {
        if (!_context.Database.IsRelational())
        {
            // El provider InMemory de las pruebas unitarias no admite FromSql.
            // Produccion/MySQL usa exclusivamente CompraId como autoridad de origen.
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

        // C2/C3 certificaron CompraId en la base de datos antes de mapearla al modelo EF.
        // Esta consulta usa directamente esa FK tipada y deja de tomar decisiones con
        // ReferenciaTipo/ReferenciaId. D2 retirara el bridge transitorio al migrar productores.
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
