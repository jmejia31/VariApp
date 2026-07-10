using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IFacturaService
{
    Task<FacturaDto?> GetByIdAsync(int id);
    Task<FacturaDto?> GetByVentaIdAsync(int ventaId);
    Task<List<FacturaDto>> GetAllAsync();
}
