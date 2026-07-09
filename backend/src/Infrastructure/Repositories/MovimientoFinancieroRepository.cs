using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class MovimientoFinancieroRepository : IMovimientoFinancieroRepository
{
    private readonly AppDbContext _context;

    public MovimientoFinancieroRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(MovimientoFinanciero movimiento) =>
        await _context.MovimientosFinancieros.AddAsync(movimiento);

    public async Task<MovimientoFinanciero?> GetByCompraIdAsync(int compraId) =>
        await _context.MovimientosFinancieros.FirstOrDefaultAsync(m => m.CompraId == compraId && m.EsAutomatico);

    public async Task<MovimientoFinanciero?> GetByVentaIdAsync(int ventaId) =>
        await _context.MovimientosFinancieros.FirstOrDefaultAsync(m => m.VentaId == ventaId && m.EsAutomatico);

    public async Task<List<MovimientoFinanciero>> GetFilteredAsync(DateTime? desde, DateTime? hasta)
    {
        var query = _context.MovimientosFinancieros.AsQueryable();
        if (desde.HasValue) query = query.Where(m => m.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(m => m.Fecha <= hasta.Value);
        return await query.OrderByDescending(m => m.Fecha).ToListAsync();
    }

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
