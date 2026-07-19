using InventoryApp.Application.Common;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario);
    Task<Usuario?> GetByIdAsync(int id);
    Task<List<Usuario>> GetAllAsync();
    Task<PagedResult<Usuario>> GetPagedAsync(PagedRequest request);
    Task<int> ContarAdministradoresActivosAsync(int? excluirUsuarioId = null);
    Task AddAsync(Usuario usuario);
    void Update(Usuario usuario);
    Task<bool> SaveChangesAsync();
}
