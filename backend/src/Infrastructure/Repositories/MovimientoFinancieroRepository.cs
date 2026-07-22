using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class MovimientoFinancieroRepository : IMovimientoFinancieroRepository
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public MovimientoFinancieroRepository(AppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    private IQueryable<MovimientoFinanciero> AplicarAlcanceActual(IQueryable<MovimientoFinanciero> query)
    {
        if (!_currentUser.EstaAutenticado || _currentUser.EsAdministrador)
            return query;

        var usuarioId = _currentUser.UsuarioId;
        return usuarioId.HasValue
            ? query.Where(m => m.CreadoPorUsuarioId == usuarioId.Value)
            : query.Where(_ => false);
    }

    public async Task AddAsync(MovimientoFinanciero movimiento) =>
        await _context.MovimientosFinancieros.AddAsync(movimiento);

    public async Task<MovimientoFinanciero?> GetByIdAsync(int id) =>
        await AplicarAlcanceActual(_context.MovimientosFinancieros).FirstOrDefaultAsync(m => m.Id == id);

    public void Update(MovimientoFinanciero movimiento) =>
        _context.MovimientosFinancieros.Update(movimiento);

    public async Task<MovimientoFinanciero?> GetByCompraIdAsync(int compraId) =>
        await AplicarAlcanceActual(_context.MovimientosFinancieros)
            .FirstOrDefaultAsync(m => m.CompraId == compraId && m.EsAutomatico);

    public async Task<MovimientoFinanciero?> GetByVentaIdAsync(int ventaId) =>
        await AplicarAlcanceActual(_context.MovimientosFinancieros)
            .FirstOrDefaultAsync(m => m.VentaId == ventaId && m.EsAutomatico);

    public async Task<List<MovimientoFinanciero>> GetFilteredAsync(DateTime? desde, DateTime? hasta)
    {
        var query = AplicarAlcanceActual(_context.MovimientosFinancieros.AsQueryable());
        if (desde.HasValue) query = query.Where(m => m.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(m => m.Fecha <= hasta.Value);
        return await query.OrderByDescending(m => m.Fecha).ToListAsync();
    }

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
