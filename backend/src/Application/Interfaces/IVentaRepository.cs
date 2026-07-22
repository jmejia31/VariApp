using InventoryApp.Application.Common;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IVentaRepository
{
    Task<Venta?> GetByIdAsync(int id);
    Task<(List<Venta> Items, int TotalCount)> GetPagedAsync(PagedRequest request);
    Task<int> GetTotalDelMesAsync(int? usuarioId = null);
    Task<decimal> GetIngresosDelMesAsync(int? usuarioId = null);
    Task<decimal> GetCuentasPorCobrarAsync(int? usuarioId = null);
    Task<decimal> GetUtilidadBrutaTotalAsync(int? usuarioId = null);
    Task<List<Venta>> GetUltimasAsync(int cantidad = 5, int? usuarioId = null);
    Task<int> ContarTodasAsync();
    Task AddAsync(Venta venta);
    void Update(Venta venta);
    Task<bool> SaveChangesAsync();
}
