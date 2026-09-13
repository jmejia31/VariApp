using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface ITemaVisualRepository
{
    Task<TemaVisual?> GetAsync();
    Task AddAsync(TemaVisual tema);
    void Update(TemaVisual tema);
    Task<bool> SaveChangesAsync();
}
