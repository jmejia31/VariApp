using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Interfaces;

public interface IProductoVarianteRepository
{
    Task<ProductoVariante?> GetByIdAsync(int id);
    Task<ProductoVariante?> GetByIdForUpdateAsync(int id);
    Task<List<ProductoVariante>> GetByIdsForUpdateAsync(IEnumerable<int> ids);
    Task<List<ProductoVariante>> GetByProductoIdAsync(int productoId, bool incluirInactivas = true);
    Task<ProductoVariante?> GetTecnicaByProductoIdAsync(int productoId, bool incluirEliminada = false);
    Task<ProductoVariante?> GetBySkuAsync(string sku);
    Task<ProductoVariante?> GetByCodigoBarrasAsync(string codigoBarras);
    Task<List<ProductoVariante>> BuscarPorCodigoAsync(
        string skuNormalizado,
        string codigoBarrasNormalizado,
        CancellationToken cancellationToken = default);
    Task<List<ProductoVariante>> BuscarPorTerminoAsync(
        string terminoNormalizado,
        bool soloConStock,
        int limite,
        CancellationToken cancellationToken = default,
        TipoInventario? tipoInventario = null);
    Task<ProductoVariante?> GetByProductoColorAsync(int productoId, int colorId);
    Task AddAsync(ProductoVariante variante);
    void Update(ProductoVariante variante);
    Task<bool> SaveChangesAsync();
}
