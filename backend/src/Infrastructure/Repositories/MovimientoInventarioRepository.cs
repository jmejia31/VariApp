using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
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

    private static IQueryable<MovimientoInventario> AplicarAlcance(
        IQueryable<MovimientoInventario> query,
        UsuarioScopeActual? alcance)
    {
        if (alcance is null)
            return query.Where(_ => false);

        return alcance.EsAdministrador
            ? query
            : query.Where(m => m.CreadoPorUsuarioId == alcance.UsuarioId);
    }

    public async Task AddAsync(MovimientoInventario movimiento) =>
        await _context.MovimientosInventario.AddAsync(movimiento);

    public async Task<List<MovimientoInventario>> GetByProductoAsync(int productoId)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        return await AplicarAlcance(_context.MovimientosInventario, alcance)
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();
    }

    public async Task<List<MovimientoInventario>> GetFilteredAsync(
        int? productoId,
        string? tipo,
        DateTime? desde,
        DateTime? hasta)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var query = AplicarAlcance(
            _context.MovimientosInventario.Include(m => m.Producto).AsQueryable(),
            alcance);

        if (productoId.HasValue) query = query.Where(m => m.ProductoId == productoId.Value);
        if (!string.IsNullOrWhiteSpace(tipo)) query = query.Where(m => m.Tipo.ToString() == tipo);
        if (desde.HasValue) query = query.Where(m => m.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(m => m.Fecha <= hasta.Value);

        return await query.OrderByDescending(m => m.Fecha).Take(200).ToListAsync();
    }
}
