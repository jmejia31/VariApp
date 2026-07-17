using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IFacturaCompartirRepository
{
    Task<EnlacePublicoFactura?> GetEnlaceVigenteAsync(int facturaId);
    Task<EnlacePublicoFactura?> GetPorTokenAsync(string token);
    Task AddEnlaceAsync(EnlacePublicoFactura enlace);
    void UpdateEnlace(EnlacePublicoFactura enlace);
    Task AddHistorialAsync(HistorialEnvioFactura historial);
    Task<List<HistorialEnvioFactura>> GetHistorialAsync(int facturaId);
    Task SaveChangesAsync();
}
