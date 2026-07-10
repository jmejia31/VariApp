using InventoryApp.Application.Common;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class VentaRepository : IVentaRepository
{
    private readonly AppDbContext _context;

    public VentaRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<Venta> ConIncludes() =>
        _context.Ventas.Include(v => v.Detalles).ThenInclude(d => d.Producto).Include(v => v.Factura);

    public async Task<Venta?> GetByIdAsync(int id) =>
        await ConIncludes().FirstOrDefaultAsync(v => v.Id == id);

    public async Task<(List<Venta> Items, int TotalCount)> GetPagedAsync(PagedRequest request)
    {
        var query = ConIncludes().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(v =>
                v.NumeroVenta.ToLower().Contains(search) ||
                v.ClienteNombre.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync();

        var sortDirDesc = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = (request.SortBy?.ToLower()) switch
        {
            "total" => sortDirDesc ? query.OrderByDescending(v => v.Total) : query.OrderBy(v => v.Total),
            "clientenombre" => sortDirDesc ? query.OrderByDescending(v => v.ClienteNombre) : query.OrderBy(v => v.ClienteNombre),
            _ => sortDirDesc ? query.OrderByDescending(v => v.Fecha) : query.OrderBy(v => v.Fecha),
        };

        var items = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<int> GetTotalDelMesAsync()
    {
        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return await _context.Ventas.CountAsync(v => v.Fecha >= inicioMes && v.Estado == EstadoDocumento.Confirmada);
    }

    public async Task<decimal> GetIngresosDelMesAsync()
    {
        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return await _context.Ventas
            .Where(v => v.Fecha >= inicioMes && v.Estado == EstadoDocumento.Confirmada)
            .SumAsync(v => (decimal?)v.Total) ?? 0m;
    }

    public async Task<decimal> GetCuentasPorCobrarAsync() =>
        await _context.Ventas
            .Where(v => v.Estado == EstadoDocumento.Confirmada && v.EstadoPago != EstadoPago.Pagado)
            .SumAsync(v => (decimal?)v.Total) ?? 0m;

    public async Task<decimal> GetUtilidadBrutaTotalAsync() =>
        await _context.Ventas
            .Where(v => v.Estado == EstadoDocumento.Confirmada)
            .SumAsync(v => (decimal?)v.UtilidadBruta) ?? 0m;

    public async Task<List<Venta>> GetUltimasAsync(int cantidad = 5) =>
        await ConIncludes().OrderByDescending(v => v.Fecha).Take(cantidad).ToListAsync();

    public async Task<int> ContarTodasAsync() =>
        await _context.Ventas.CountAsync();

    public async Task AddAsync(Venta venta) =>
        await _context.Ventas.AddAsync(venta);

    public void Update(Venta venta) =>
        _context.Ventas.Update(venta);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
