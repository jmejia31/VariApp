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

    public Task<List<ProductoVariante>> GetByProductoIdAsync(int productoId, bool incluirInactivas = true)
    {
        var query = Query().Where(v => v.ProductoId == productoId && !v.Eliminado);
        if (!incluirInactivas)
            query = query.Where(v => v.Activo);
        return query.OrderBy(v => v.Color!.Nombre).ThenBy(v => v.Sku).ToListAsync();
    }

    public Task<ProductoVariante?> GetBySkuAsync(string sku) =>
        Query().FirstOrDefaultAsync(v => !v.Eliminado && v.Sku == sku);

    public Task<ProductoVariante?> GetByCodigoBarrasAsync(string codigoBarras) =>
        Query().FirstOrDefaultAsync(v => !v.Eliminado && v.CodigoBarras == codigoBarras);

    public Task<ProductoVariante?> GetByProductoColorAsync(int productoId, int colorId) =>
        Query().FirstOrDefaultAsync(v => !v.Eliminado && v.ProductoId == productoId && v.ColorId == colorId);

    public Task AddAsync(ProductoVariante variante) =>
        _context.ProductoVariantes.AddAsync(variante).AsTask();

    public void Update(ProductoVariante variante) =>
        _context.ProductoVariantes.Update(variante);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
