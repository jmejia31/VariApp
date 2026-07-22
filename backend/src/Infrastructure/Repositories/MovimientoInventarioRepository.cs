using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class MovimientoInventarioRepository : IMovimientoInventarioRepository
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public MovimientoInventarioRepository(AppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    private IQueryable<MovimientoInventario> AplicarAlcanceActual(IQueryable<MovimientoInventario> query)
    {
        if (!_currentUser.EstaAutenticado || _currentUser.EsAdministrador)
            return query;

        var usuarioId = _currentUser.UsuarioId;
        return usuarioId.HasValue
            ? query.Where(m => m.CreadoPorUsuarioId == usuarioId.Value)
            : query.Where(_ => false);
    }

    public async Task AddAsync(MovimientoInventario movimiento) =>
        await _context.MovimientosInventario.AddAsync(movimiento);

    public async Task<List<MovimientoInventario>> GetByProductoAsync(int productoId) =>
        await AplicarAlcanceActual(_context.MovimientosInventario)
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();

    public async Task<List<MovimientoInventario>> GetFilteredAsync(int? productoId, string? tipo, DateTime? desde, DateTime? hasta)
    {
        var query = AplicarAlcanceActual(_context.MovimientosInventario.Include(m => m.Producto).AsQueryable());

        if (productoId.HasValue) query = query.Where(m => m.ProductoId == productoId.Value);
        if (!string.IsNullOrWhiteSpace(tipo)) query = query.Where(m => m.Tipo.ToString() == tipo);
        if (desde.HasValue) query = query.Where(m => m.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(m => m.Fecha <= hasta.Value);

        return await query.OrderByDescending(m => m.Fecha).Take(200).ToListAsync();
    }
}
