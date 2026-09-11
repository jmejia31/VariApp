using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IPedidoVentaRepository
{
    Task<PedidoVenta?> GetByIdAsync(int id, bool asNoTracking = false);
    Task<PedidoVenta?> GetByIdForUpdateAsync(int id);
    Task<PedidoVenta?> GetByCotizacionIdForUpdateAsync(int cotizacionId);
    Task<PedidoVenta?> GetByIdempotencyKeyForUpdateAsync(string idempotencyKey);
    Task<(List<PedidoVenta> Items, int Total)> GetPagedAsync(PedidoVentaFiltroDto request);
    Task AddAsync(PedidoVenta pedido);
    void Update(PedidoVenta pedido);
    Task<bool> SaveChangesAsync();
}
