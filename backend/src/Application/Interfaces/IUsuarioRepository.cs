using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario);
}
