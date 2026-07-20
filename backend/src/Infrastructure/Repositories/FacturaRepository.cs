using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class FacturaRepository : IFacturaRepository
{
    private readonly AppDbContext _context;

    public FacturaRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<Factura> ConIncludes() =>
        _context.Facturas
            .Include(f => f.Detalles)
            .Include(f => f.Venta)
                .ThenInclude(v => v!.DescuentosAplicados)
            .Include(f => f.Venta)
                .ThenInclude(v => v!.ImpuestosAplicados);

    public async Task<Factura?> GetByIdAsync(int id) =>
        await ConIncludes().FirstOrDefaultAsync(f => f.Id == id);

    public async Task<Factura?> GetByVentaIdAsync(int ventaId) =>
        await ConIncludes().FirstOrDefaultAsync(f => f.VentaId == ventaId);

    public async Task<List<Factura>> GetAllAsync() =>
        await ConIncludes().OrderByDescending(f => f.FechaEmision).ToListAsync();

    public async Task<int> ContarTodasAsync() =>
        await _context.Facturas.CountAsync();

    public async Task AddAsync(Factura factura) =>
        await _context.Facturas.AddAsync(factura);

    public void Update(Factura factura) =>
        _context.Facturas.Update(factura);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
