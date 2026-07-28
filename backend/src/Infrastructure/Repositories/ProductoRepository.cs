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

    private IQueryable<Producto> ConIncludes() =>
        _context.Productos
            .Include(p => p.Imagenes)
            .Include(p => p.Categoria)
            .Include(p => p.Color)
            .Include(p => p.Talla)
            .Include(p => p.MarcaCatalogo)
            .Include(p => p.ModeloCatalogo)
            .Include(p => p.Variantes.Where(v => !v.Eliminado))
                .ThenInclude(v => v.Color)
            .AsSplitQuery();

    public async Task<Producto?> GetByIdAsync(int id) =>
        await ConIncludes().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<(List<Producto> Items, int TotalCount)> GetPagedAsync(PagedRequest request)
    {
        var query = ConIncludes().AsQueryable();

        if (request is ProductoPagedRequest filters)
        {
            if (filters.CategoriaId.HasValue)
                query = query.Where(p => p.CategoriaId == filters.CategoriaId.Value);
            if (filters.ColorId.HasValue)
                query = query.Where(p => p.ColorId == filters.ColorId.Value ||
                    p.Variantes.Any(v => !v.Eliminado && v.ColorId == filters.ColorId.Value));
            if (filters.TallaId.HasValue)
                query = query.Where(p => p.TallaId == filters.TallaId.Value);
            if (filters.MarcaId.HasValue)
                query = query.Where(p => p.MarcaId == filters.MarcaId.Value);
            if (filters.ModeloId.HasValue)
                query = query.Where(p => p.ModeloId == filters.ModeloId.Value);
            if (filters.Activo.HasValue)
                query = query.Where(p => p.Activo == filters.Activo.Value);
            if (filters.Agotado.HasValue)
                query = filters.Agotado.Value
                    ? query.Where(p => p.Cantidad <= 0)
                    : query.Where(p => p.Cantidad > 0);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(search) ||
                p.Marca.ToLower().Contains(search) ||
                p.Modelo.ToLower().Contains(search) ||
                (p.MarcaCatalogo != null && p.MarcaCatalogo.Nombre.ToLower().Contains(search)) ||
                (p.ModeloCatalogo != null && p.ModeloCatalogo.Nombre.ToLower().Contains(search)) ||
                (p.Color != null && p.Color.Nombre.ToLower().Contains(search)) ||
                (p.Talla != null && p.Talla.Nombre.ToLower().Contains(search)) ||
                p.Variantes.Any(v => !v.Eliminado &&
                    ((v.Sku != null && v.Sku.ToLower().Contains(search)) ||
                     (v.CodigoBarras != null && v.CodigoBarras.ToLower().Contains(search)) ||
                     (v.Color != null && v.Color.Nombre.ToLower().Contains(search)))));
        }

        var totalCount = await query.CountAsync();

        var sortDirDesc = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = request.SortBy?.ToLower() switch
        {
            "marca" => sortDirDesc
                ? query.OrderByDescending(p => p.MarcaCatalogo != null ? p.MarcaCatalogo.Nombre : p.Marca)
                : query.OrderBy(p => p.MarcaCatalogo != null ? p.MarcaCatalogo.Nombre : p.Marca),
            "modelo" => sortDirDesc
                ? query.OrderByDescending(p => p.ModeloCatalogo != null ? p.ModeloCatalogo.Nombre : p.Modelo)
                : query.OrderBy(p => p.ModeloCatalogo != null ? p.ModeloCatalogo.Nombre : p.Modelo),
            "color" => sortDirDesc
                ? query.OrderByDescending(p => p.Color != null ? p.Color.Nombre : string.Empty)
                : query.OrderBy(p => p.Color != null ? p.Color.Nombre : string.Empty),
            "talla" => sortDirDesc
                ? query.OrderByDescending(p => p.Talla != null ? p.Talla.Nombre : string.Empty)
                : query.OrderBy(p => p.Talla != null ? p.Talla.Nombre : string.Empty),
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
        await ConIncludes()
            .Where(p => p.Cantidad <= 0)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

    public async Task<List<Producto>> GetUltimosAgregadosAsync(int cantidad = 5) =>
        await ConIncludes()
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
