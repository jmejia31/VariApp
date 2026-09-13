using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface ICotizacionRepository
{
    Task<Cotizacion?> GetByIdAsync(int id, bool asNoTracking = false);
    Task<Cotizacion?> GetByIdForUpdateAsync(int id);
    Task<(List<Cotizacion> Items, int Total)> GetPagedAsync(CotizacionFiltroDto request);
    Task AddAsync(Cotizacion cotizacion);
    void Update(Cotizacion cotizacion);
    void Remove(Cotizacion cotizacion);
    Task<bool> SaveChangesAsync();
}
