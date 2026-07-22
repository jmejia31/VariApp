using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class FacturaRepository : IFacturaRepository
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public FacturaRepository(AppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    private IQueryable<Factura> ConIncludes() =>
        _context.Facturas
            .Include(f => f.Detalles)
            .Include(f => f.Venta)
                .ThenInclude(v => v!.DescuentosAplicados)
            .Include(f => f.Venta)
                .ThenInclude(v => v!.ImpuestosAplicados);

    private IQueryable<Factura> AplicarAlcanceActual(IQueryable<Factura> query)
    {
        if (!_currentUser.EstaAutenticado || _currentUser.EsAdministrador)
            return query;

        var usuarioId = _currentUser.UsuarioId;
        return usuarioId.HasValue
            ? query.Where(f => f.VendedorUsuarioId == usuarioId.Value || f.GeneradaPorUsuarioId == usuarioId.Value)
            : query.Where(_ => false);
    }

    public async Task<Factura?> GetByIdAsync(int id) =>
        await AplicarAlcanceActual(ConIncludes()).FirstOrDefaultAsync(f => f.Id == id);

    public async Task<Factura?> GetByVentaIdAsync(int ventaId) =>
        await AplicarAlcanceActual(ConIncludes()).FirstOrDefaultAsync(f => f.VentaId == ventaId);

    public async Task<List<Factura>> GetAllAsync() =>
        await AplicarAlcanceActual(ConIncludes()).OrderByDescending(f => f.FechaEmision).ToListAsync();

    public async Task<int> ContarTodasAsync() =>
        await _context.Facturas.CountAsync();

    public async Task AddAsync(Factura factura) =>
        await _context.Facturas.AddAsync(factura);

    public void Update(Factura factura) =>
        _context.Facturas.Update(factura);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
