using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IClienteRepository
{
    Task<Cliente?> GetByIdAsync(int id);
    Task<Cliente?> GetByIdConVentasAsync(int id);
    Task<List<Cliente>> GetAllAsync();
    Task<List<Cliente>> GetActivosAsync();
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null);
    Task AddAsync(Cliente cliente);
    void Update(Cliente cliente);
    void Remove(Cliente cliente);
    Task<bool> SaveChangesAsync();
}
