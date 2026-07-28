using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IProductoVarianteRepository
{
    Task<ProductoVariante?> GetByIdAsync(int id);
    Task<List<ProductoVariante>> GetByProductoIdAsync(int productoId, bool incluirInactivas = true);
    Task<ProductoVariante?> GetBySkuAsync(string sku);
    Task<ProductoVariante?> GetByCodigoBarrasAsync(string codigoBarras);
    Task<ProductoVariante?> GetByProductoColorAsync(int productoId, int colorId);
    Task AddAsync(ProductoVariante variante);
    void Update(ProductoVariante variante);
    Task<bool> SaveChangesAsync();
}
