using InventoryApp.Application.Common;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface ICompraRepository
{
    Task<Compra?> GetByIdAsync(int id);
    Task<(List<Compra> Items, int TotalCount)> GetPagedAsync(PagedRequest request);
    Task<int> GetTotalDelMesAsync();
    Task<decimal> GetCuentasPorPagarAsync();
    Task<List<Compra>> GetUltimasAsync(int cantidad = 5);
    Task<int> ContarTodasAsync();
    Task AddAsync(Compra compra);
    void Update(Compra compra);
    Task<bool> SaveChangesAsync();
}
