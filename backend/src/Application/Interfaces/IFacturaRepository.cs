using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IFacturaRepository
{
    Task<Factura?> GetByIdAsync(int id);
    Task<Factura?> GetByVentaIdAsync(int ventaId);
    Task<List<Factura>> GetAllAsync();
    Task<int> ContarTodasAsync();
    Task AddAsync(Factura factura);
    void Update(Factura factura);
    Task<bool> SaveChangesAsync();
}
