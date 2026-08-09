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
                .ThenInclude(v => v.Marca)
            .Include(p => p.Variantes.Where(v => !v.Eliminado))
                .ThenInclude(v => v.Modelo)
            .Include(p => p.Variantes.Where(v => !v.Eliminado))
                .ThenInclude(v => v.Color)
            .Include(p => p.Variantes.Where(v => !v.Eliminado))
                .ThenInclude(v => v.Talla)
            .AsSplitQuery();

    public async Task<Producto?> GetByIdAsync(int id) =>
        await ConIncludes().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Producto?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        return await _context.Productos
            .FromSqlInterpolated($"SELECT p.* FROM Productos p WHERE p.Id = {id} AND p.Eliminado = 0 FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<List<Producto>> GetByIdsForUpdateAsync(IEnumerable<int> ids)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdsForUpdateAsync requiere una transacción activa.");

        var result = new List<Producto>();
        foreach (var id in ids.Distinct().OrderBy(x => x))
        {
            var p = await GetByIdForUpdateAsync(id);
            if (p != null) result.Add(p);
        }
        return result;
    }

    public async Task<(List<Producto> Items, int TotalCount)> GetPagedAsync(PagedRequest request)
    {
        var query = ConIncludes().AsQueryable();

        if (request is ProductoPagedRequest filters)
        {
            if (filters.CategoriaId.HasValue)
                query = query.Where(p => p.CategoriaId == filters.CategoriaId.Value);
            if (filters.ColorId.HasValue)
                query = query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.ColorId == filters.ColorId.Value));
            if (filters.TallaId.HasValue)
                query = query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.TallaId == filters.TallaId.Value));
            if (filters.MarcaId.HasValue)
                query = query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.MarcaId == filters.MarcaId.Value));
            if (filters.ModeloId.HasValue)
                query = query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.ModeloId == filters.ModeloId.Value));
            if (filters.Activo.HasValue)
                query = query.Where(p => p.Activo == filters.Activo.Value);
            if (filters.Agotado.HasValue)
                query = filters.Agotado.Value
                    ? query.Where(p => !p.Variantes.Any(v => !v.Eliminado && v.Activo && v.Cantidad > 0))
                    : query.Where(p => p.Variantes.Any(v => !v.Eliminado && v.Activo && v.Cantidad > 0));
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
                p.Variantes.Any(v => !v.Eliminado &&
                    ((v.Sku != null && v.Sku.ToLower().Contains(search)) ||
                     (v.CodigoBarras != null && v.CodigoBarras.ToLower().Contains(search)) ||
                     (v.Marca != null && v.Marca.Nombre.ToLower().Contains(search)) ||
                     (v.Modelo != null && v.Modelo.Nombre.ToLower().Contains(search)) ||
                     (v.Color != null && v.Color.Nombre.ToLower().Contains(search)) ||
                     (v.Talla != null && v.Talla.Nombre.ToLower().Contains(search)))));
        }

        var totalCount = await query.CountAsync();

        var sortDirDesc = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = request.SortBy?.ToLower() switch
        {
            "marca" => sortDirDesc
                ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Marca != null ? v.Marca.Nombre : string.Empty).FirstOrDefault())
                : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Marca != null ? v.Marca.Nombre : string.Empty).FirstOrDefault()),
            "modelo" => sortDirDesc
                ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Modelo != null ? v.Modelo.Nombre : string.Empty).FirstOrDefault())
                : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Modelo != null ? v.Modelo.Nombre : string.Empty).FirstOrDefault()),
            "color" => sortDirDesc
                ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Color != null ? v.Color.Nombre : string.Empty).FirstOrDefault())
                : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Color != null ? v.Color.Nombre : string.Empty).FirstOrDefault()),
            "talla" => sortDirDesc
                ? query.OrderByDescending(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Talla != null ? v.Talla.Nombre : string.Empty).FirstOrDefault())
                : query.OrderBy(p => p.Variantes.Where(v => !v.Eliminado).Select(v => v.Talla != null ? v.Talla.Nombre : string.Empty).FirstOrDefault()),
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
            .Where(p => !p.Variantes.Any(v => !v.Eliminado && v.Activo && v.Cantidad > v.UmbralStockBajo))
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
        await _context.ProductoVariantes.Where(v => !v.Eliminado).SumAsync(v => (int?)v.Cantidad) ?? 0;

    public async Task<decimal> GetValorTotalCostoAsync() =>
        await _context.ProductoVariantes
            .Where(v => !v.Eliminado)
            .SumAsync(v => (decimal?)((v.Costo ?? 0m) * v.Cantidad)) ?? 0m;

    public async Task<decimal> GetValorTotalPrecioAsync() =>
        await _context.ProductoVariantes
            .Where(v => !v.Eliminado)
            .SumAsync(v => (decimal?)((v.Precio ?? 0m) * v.Cantidad)) ?? 0m;

    public async Task AddAsync(Producto producto) =>
        await _context.Productos.AddAsync(producto);

    public void Update(Producto producto) =>
        _context.Productos.Update(producto);

    public void Remove(Producto producto) =>
        _context.Productos.Remove(producto);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
