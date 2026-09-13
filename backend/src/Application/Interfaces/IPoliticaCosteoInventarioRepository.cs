using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IPoliticaCosteoInventarioRepository
{
    Task<PoliticaCosteoInventario?> GetVigenteAsync(int empresaConfiguracionId, bool tracking = false);
    Task<(IReadOnlyList<PoliticaCosteoInventario> Items, int Total)> GetHistorialAsync(
        int empresaConfiguracionId,
        PoliticaCosteoInventarioQueryDto query);
    Task AddAsync(PoliticaCosteoInventario politica);
    Task SaveChangesAsync();
}
