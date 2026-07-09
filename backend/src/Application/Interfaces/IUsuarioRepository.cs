using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario);
    Task<Usuario?> GetByIdAsync(int id);
    Task<List<Usuario>> GetAllAsync();
    Task AddAsync(Usuario usuario);
    void Update(Usuario usuario);
    Task<bool> SaveChangesAsync();
}
