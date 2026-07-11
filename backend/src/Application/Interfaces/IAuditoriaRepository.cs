using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IAuditoriaRepository
{
    Task AddAsync(RegistroAuditoria registro);
    Task<(List<RegistroAuditoria> Items, int TotalCount)> GetFilteredAsync(
        int? usuarioId, string? modulo, string? accion, DateTime? desde, DateTime? hasta, int page, int pageSize);
    Task<bool> SaveChangesAsync();
}
