using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class MovimientoFinancieroRepository : IMovimientoFinancieroRepository
{
    private readonly AppDbContext _context;
    private readonly IUsuarioScopeService _usuarioScope;

    public MovimientoFinancieroRepository(AppDbContext context, IUsuarioScopeService usuarioScope)
    {
        _context = context;
        _usuarioScope = usuarioScope;
    }

    private static IQueryable<MovimientoFinanciero> AplicarAlcance(
        IQueryable<MovimientoFinanciero> query,
        UsuarioScopeActual? alcance)
    {
        if (alcance is null)
            return query.Where(_ => false);

        return alcance.EsAdministrador
            ? query
            : query.Where(m => m.CreadoPorUsuarioId == alcance.UsuarioId);
    }

    public async Task AddAsync(MovimientoFinanciero movimiento) =>
        await _context.MovimientosFinancieros.AddAsync(movimiento);

    public async Task<MovimientoFinanciero?> GetByIdAsync(int id)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        return await AplicarAlcance(_context.MovimientosFinancieros, alcance)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public void Update(MovimientoFinanciero movimiento) =>
        _context.MovimientosFinancieros.Update(movimiento);

    public async Task<MovimientoFinanciero?> GetByCompraIdAsync(int compraId)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        return await AplicarAlcance(_context.MovimientosFinancieros, alcance)
            .FirstOrDefaultAsync(m => m.CompraId == compraId && m.EsAutomatico);
    }

    public async Task<MovimientoFinanciero?> GetByVentaIdAsync(int ventaId)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        return await AplicarAlcance(_context.MovimientosFinancieros, alcance)
            .FirstOrDefaultAsync(m => m.VentaId == ventaId && m.EsAutomatico);
    }

    public async Task<List<MovimientoFinanciero>> GetFilteredAsync(DateTime? desde, DateTime? hasta)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var query = AplicarAlcance(_context.MovimientosFinancieros.AsQueryable(), alcance);
        if (desde.HasValue) query = query.Where(m => m.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(m => m.Fecha <= hasta.Value);
        return await query.OrderByDescending(m => m.Fecha).ToListAsync();
    }

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
