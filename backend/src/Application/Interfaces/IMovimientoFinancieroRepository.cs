using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IMovimientoFinancieroRepository
{
    Task AddAsync(MovimientoFinanciero movimiento);
    Task<MovimientoFinanciero?> GetByCompraIdAsync(int compraId);
    Task<MovimientoFinanciero?> GetByVentaIdAsync(int ventaId);
    Task<List<MovimientoFinanciero>> GetFilteredAsync(DateTime? desde, DateTime? hasta);
    Task<bool> SaveChangesAsync();
}
