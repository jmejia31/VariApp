using InventoryApp.Application.Common;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class ProductoRepository : IProductoRepository
{
    private readonly AppDbContext _context;

    public ProductoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Producto?> GetByIdAsync(int id) =>
        await _context.Productos.FindAsync(id);

    public async Task<(List<Producto> Items, int TotalCount)> GetPagedAsync(PagedRequest request)
    {
        var query = _context.Productos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(search) ||
                p.Marca.ToLower().Contains(search) ||
                p.Modelo.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync();

        var sortDirDesc = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = request.SortBy?.ToLower() switch
        {
            "marca" => sortDirDesc ? query.OrderByDescending(p => p.Marca) : query.OrderBy(p => p.Marca),
            "modelo" => sortDirDesc ? query.OrderByDescending(p => p.Modelo) : query.OrderBy(p => p.Modelo),
            "cantidad" => sortDirDesc ? query.OrderByDescending(p => p.Cantidad) : query.OrderBy(p => p.Cantidad),
            "costo" => sortDirDesc ? query.OrderByDescending(p => p.Costo) : query.OrderBy(p => p.Costo),
            "precio" => sortDirDesc ? query.OrderByDescending(p => p.Precio) : query.OrderBy(p => p.Precio),
            _ => sortDirDesc ? query.OrderByDescending(p => p.Nombre) : query.OrderBy(p => p.Nombre),
        };

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<Producto>> GetStockBajoAsync() =>
        await _context.Productos
            .Where(p => p.Cantidad < p.UmbralStockBajo)
            .OrderBy(p => p.Cantidad)
            .ToListAsync();

    public async Task<List<Producto>> GetUltimosAgregadosAsync(int cantidad = 5) =>
        await _context.Productos
            .OrderByDescending(p => p.FechaCreacion)
            .Take(cantidad)
            .ToListAsync();

    public async Task<int> GetTotalProductosAsync() =>
        await _context.Productos.CountAsync();

    public async Task<int> GetTotalUnidadesAsync() =>
        await _context.Productos.SumAsync(p => (int?)p.Cantidad) ?? 0;

    public async Task<decimal> GetValorTotalCostoAsync() =>
        await _context.Productos.SumAsync(p => (decimal?)(p.Costo * p.Cantidad)) ?? 0m;

    public async Task<decimal> GetValorTotalPrecioAsync() =>
        await _context.Productos.SumAsync(p => (decimal?)(p.Precio * p.Cantidad)) ?? 0m;

    public async Task AddAsync(Producto producto) =>
        await _context.Productos.AddAsync(producto);

    public void Update(Producto producto) =>
        _context.Productos.Update(producto);

    public void Remove(Producto producto) =>
        _context.Productos.Remove(producto);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
