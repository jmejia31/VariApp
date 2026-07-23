using InventoryApp.Application.Common;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class FacturaRepository : IFacturaRepository
{
    private readonly AppDbContext _context;
    private readonly IUsuarioScopeService _usuarioScope;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public FacturaRepository(
        AppDbContext context,
        IUsuarioScopeService usuarioScope,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _context = context;
        _usuarioScope = usuarioScope;
        _httpContextAccessor = httpContextAccessor;
    }

    private IQueryable<Factura> ConIncludes() =>
        _context.Facturas
            .Include(f => f.Detalles)
            .Include(f => f.Venta)
                .ThenInclude(v => v!.DescuentosAplicados)
            .Include(f => f.Venta)
                .ThenInclude(v => v!.ImpuestosAplicados);

    private static IQueryable<Factura> AplicarAlcance(
        IQueryable<Factura> query,
        UsuarioScopeActual? alcance)
    {
        if (alcance is null)
            return query.Where(_ => false);

        return alcance.EsAdministrador
            ? query
            : query.Where(f =>
                f.VendedorUsuarioId == alcance.UsuarioId ||
                f.GeneradaPorUsuarioId == alcance.UsuarioId ||
                (f.Venta != null && f.Venta.CreadoPorUsuarioId == alcance.UsuarioId));
    }

    public async Task<Factura?> GetByIdAsync(int id)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        if (alcance is null && TieneAccesoPublicoValidado(id))
            return await GetByIdParaEnlacePublicoValidadoAsync(id);

        return await AplicarAlcance(ConIncludes(), alcance)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Factura?> GetByVentaIdAsync(int ventaId)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        return await AplicarAlcance(ConIncludes(), alcance)
            .FirstOrDefaultAsync(f => f.VentaId == ventaId);
    }

    public async Task<List<Factura>> GetAllAsync()
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        return await AplicarAlcance(ConIncludes(), alcance)
            .OrderByDescending(f => f.FechaEmision)
            .ToListAsync();
    }

    public async Task<Factura?> GetByIdParaEnlacePublicoValidadoAsync(int id) =>
        TieneAccesoPublicoValidado(id)
            ? await ConIncludes().FirstOrDefaultAsync(f => f.Id == id)
            : null;

    public async Task<int> ContarTodasAsync() =>
        await _context.Facturas.CountAsync();

    public async Task AddAsync(Factura factura) =>
        await _context.Facturas.AddAsync(factura);

    public void Update(Factura factura) =>
        _context.Facturas.Update(factura);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;

    private bool TieneAccesoPublicoValidado(int facturaId)
    {
        var items = _httpContextAccessor?.HttpContext?.Items;
        return items is not null &&
               items.TryGetValue(PublicInvoiceAccessContext.FacturaIdKey, out var value) &&
               value is int autorizado &&
               autorizado == facturaId;
    }
}
