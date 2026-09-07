using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IFacturaProveedorRepository
{
    Task<(IReadOnlyList<FacturaProveedor> Items, int Total)> GetPagedAsync(FacturaProveedorFiltroDto filtro);
    Task<FacturaProveedor?> GetByIdAsync(int id, bool tracking = false);
    Task<FacturaProveedor?> GetByIdForUpdateAsync(int id);
    Task<FacturaProveedor?> GetByProveedorNumeroAsync(int proveedorId, string numeroFactura, bool tracking = false);
    Task<decimal> GetCantidadRegistradaAcumuladaPorDetalleAsync(int ordenCompraDetalleId, int? excluirFacturaId = null);
    Task AddAsync(FacturaProveedor factura);
    Task SaveChangesAsync();
}
