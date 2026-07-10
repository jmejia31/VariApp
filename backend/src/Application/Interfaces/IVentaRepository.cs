using InventoryApp.Application.Common;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IVentaRepository
{
    Task<Venta?> GetByIdAsync(int id);
    Task<(List<Venta> Items, int TotalCount)> GetPagedAsync(PagedRequest request);
    Task<int> GetTotalDelMesAsync();
    Task<decimal> GetIngresosDelMesAsync();
    Task<decimal> GetCuentasPorCobrarAsync();
    Task<decimal> GetUtilidadBrutaTotalAsync();
    Task<List<Venta>> GetUltimasAsync(int cantidad = 5);
    Task<int> ContarTodasAsync();
    Task AddAsync(Venta venta);
    void Update(Venta venta);
    Task<bool> SaveChangesAsync();
}
