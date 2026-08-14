using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class AjusteInventarioRepository : IAjusteInventarioRepository
{
    private readonly AppDbContext _context;

    public AjusteInventarioRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<AjusteInventario> ConDetalles() =>
        _context.AjustesInventario
            .Include(a => a.Detalles)
            .AsSplitQuery();

    public async Task<List<AjusteInventario>> GetAllAsync() =>
        await ConDetalles()
            .OrderByDescending(a => a.FechaAjuste)
            .ThenByDescending(a => a.Id)
            .ToListAsync();

    public async Task<AjusteInventario?> GetByIdAsync(int id) =>
        await ConDetalles().FirstOrDefaultAsync(a => a.Id == id);

    public async Task<AjusteInventario?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        var ajuste = await _context.AjustesInventario
            .FromSqlInterpolated($"SELECT a.* FROM AjustesInventario a WHERE a.Id = {id} AND a.Eliminado = 0 FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();

        if (ajuste is not null)
        {
            await _context.Entry(ajuste)
                .Collection(a => a.Detalles)
                .LoadAsync();
        }

        return ajuste;
    }

    public async Task AddAsync(AjusteInventario ajuste) =>
        await _context.AjustesInventario.AddAsync(ajuste);

    public void Update(AjusteInventario ajuste) =>
        _context.AjustesInventario.Update(ajuste);

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
