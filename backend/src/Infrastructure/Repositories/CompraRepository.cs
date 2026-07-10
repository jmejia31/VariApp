using InventoryApp.Application.Common;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class CompraRepository : ICompraRepository
{
    private readonly AppDbContext _context;

    public CompraRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<Compra> ConIncludes() =>
        _context.Compras.Include(c => c.Detalles).ThenInclude(d => d.Producto);

    public async Task<Compra?> GetByIdAsync(int id) =>
        await ConIncludes().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<(List<Compra> Items, int TotalCount)> GetPagedAsync(PagedRequest request)
    {
        var query = ConIncludes().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(c =>
                c.NumeroCompra.ToLower().Contains(search) ||
                c.ProveedorNombre.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync();

        var sortDirDesc = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = (request.SortBy?.ToLower()) switch
        {
            "total" => sortDirDesc ? query.OrderByDescending(c => c.Total) : query.OrderBy(c => c.Total),
            "proveedornombre" => sortDirDesc ? query.OrderByDescending(c => c.ProveedorNombre) : query.OrderBy(c => c.ProveedorNombre),
            _ => sortDirDesc ? query.OrderByDescending(c => c.Fecha) : query.OrderBy(c => c.Fecha),
        };

        var items = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<int> GetTotalDelMesAsync()
    {
        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return await _context.Compras.CountAsync(c => c.Fecha >= inicioMes && c.Estado == Domain.Enums.EstadoDocumento.Confirmada);
    }

    public async Task<decimal> GetCuentasPorPagarAsync() =>
        await _context.Compras
            .Where(c => c.Estado == Domain.Enums.EstadoDocumento.Confirmada && c.EstadoPago != Domain.Enums.EstadoPago.Pagado)
            .SumAsync(c => (decimal?)c.Total) ?? 0m;

    public async Task<List<Compra>> GetUltimasAsync(int cantidad = 5) =>
        await ConIncludes().OrderByDescending(c => c.Fecha).Take(cantidad).ToListAsync();

    public async Task<int> ContarTodasAsync() =>
        await _context.Compras.CountAsync();

    public async Task AddAsync(Compra compra) =>
        await _context.Compras.AddAsync(compra);

    public void Update(Compra compra) =>
        _context.Compras.Update(compra);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
