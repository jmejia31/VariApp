using InventoryApp.Application.Common;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IProductoRepository
{
    Task<Producto?> GetByIdAsync(int id);
    Task<(List<Producto> Items, int TotalCount)> GetPagedAsync(PagedRequest request);
    Task<List<Producto>> GetStockBajoAsync();
    Task<List<Producto>> GetUltimosAgregadosAsync(int cantidad = 5);
    Task<int> GetTotalProductosAsync();
    Task<int> GetTotalUnidadesAsync();
    Task<decimal> GetValorTotalCostoAsync();
    Task<decimal> GetValorTotalPrecioAsync();
    Task AddAsync(Producto producto);
    void Update(Producto producto);
    void Remove(Producto producto);
    Task<bool> SaveChangesAsync();
}
