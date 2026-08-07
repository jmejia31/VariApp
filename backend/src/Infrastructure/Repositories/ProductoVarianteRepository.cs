using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class ProductoVarianteRepository : IProductoVarianteRepository
{
    private readonly AppDbContext _context;

    public ProductoVarianteRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<ProductoVariante> Query() =>
        _context.ProductoVariantes
            .Include(v => v.Producto)
            .Include(v => v.Color);

    public Task<ProductoVariante?> GetByIdAsync(int id) =>
        Query().FirstOrDefaultAsync(v => v.Id == id && !v.Eliminado);

    public async Task<ProductoVariante?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        return await _context.ProductoVariantes
            .FromSqlInterpolated($"SELECT pv.* FROM ProductoVariantes pv WHERE pv.Id = {id} AND pv.Eliminado = 0 FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<List<ProductoVariante>> GetByIdsForUpdateAsync(IEnumerable<int> ids)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdsForUpdateAsync requiere una transacción activa.");

        var result = new List<ProductoVariante>();
        foreach (var id in ids.Distinct().OrderBy(x => x))
        {
            var v = await GetByIdForUpdateAsync(id);
            if (v != null) result.Add(v);
        }
        return result;
    }

    public Task<List<ProductoVariante>> GetByProductoIdAsync(int productoId, bool incluirInactivas = true)
    {
        var query = Query().Where(v => v.ProductoId == productoId && !v.Eliminado);
        if (!incluirInactivas)
            query = query.Where(v => v.Activo);
        return query.OrderBy(v => v.Color!.Nombre).ThenBy(v => v.Sku).ToListAsync();
    }

    public Task<ProductoVariante?> GetTecnicaByProductoIdAsync(
        int productoId,
        bool incluirEliminada = false)
    {
        var query = Query()
            .IgnoreQueryFilters()
            .Where(v => v.ProductoId == productoId && v.EsTecnica);
        if (!incluirEliminada)
            query = query.Where(v => !v.Eliminado);

        return query
            .OrderByDescending(v => v.Id)
            .FirstOrDefaultAsync();
    }

    public Task<ProductoVariante?> GetBySkuAsync(string sku) =>
        Query().FirstOrDefaultAsync(v => !v.Eliminado && v.Sku == sku);

    public Task<ProductoVariante?> GetByCodigoBarrasAsync(string codigoBarras) =>
        Query().FirstOrDefaultAsync(v => !v.Eliminado && v.CodigoBarras == codigoBarras);

    public Task<List<ProductoVariante>> BuscarPorCodigoAsync(
        string skuNormalizado,
        string codigoBarrasNormalizado,
        CancellationToken cancellationToken = default) =>
        _context.ProductoVariantes
            .AsNoTracking()
            .Include(v => v.Producto)
            .Include(v => v.Color)
            .Where(v =>
                !v.Eliminado &&
                !v.Producto.Eliminado &&
                ((v.Sku != null && v.Sku == skuNormalizado) ||
                 (v.CodigoBarras != null && v.CodigoBarras == codigoBarrasNormalizado)))
            .OrderBy(v => v.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

    public Task<ProductoVariante?> GetByProductoColorAsync(int productoId, int colorId) =>
        Query().FirstOrDefaultAsync(v => !v.Eliminado && v.ProductoId == productoId && v.ColorId == colorId);

    public Task AddAsync(ProductoVariante variante) =>
        _context.ProductoVariantes.AddAsync(variante).AsTask();

    public void Update(ProductoVariante variante) =>
        _context.ProductoVariantes.Update(variante);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
