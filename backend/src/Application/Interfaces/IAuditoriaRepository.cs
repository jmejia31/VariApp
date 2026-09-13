using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IAuditoriaRepository
{
    Task AddAsync(RegistroAuditoria registro);
    Task<(List<RegistroAuditoria> Items, int TotalCount)> GetFilteredAsync(AuditoriaFiltroDto filtro);
    Task<bool> SaveChangesAsync();
}
