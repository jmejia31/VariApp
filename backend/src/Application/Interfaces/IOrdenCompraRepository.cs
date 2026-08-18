using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IOrdenCompraRepository
{
    Task<(IReadOnlyList<OrdenCompra> Items, int Total)> GetPagedAsync(OrdenCompraFiltroDto filtro);
    Task<OrdenCompra?> GetByIdAsync(int id, bool tracking = false);
    Task<OrdenCompra?> GetByIdForUpdateAsync(int id);
    Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null);
    Task AddAsync(OrdenCompra orden);
    Task SaveChangesAsync();
}
