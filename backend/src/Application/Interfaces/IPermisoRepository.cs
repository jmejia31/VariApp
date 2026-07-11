using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Interfaces;

public interface IPermisoRepository
{
    Task<List<RolPermiso>> GetAllAsync();
    Task<RolPermiso?> GetAsync(RolUsuario rol, string modulo);
    Task AddAsync(RolPermiso permiso);
    void Update(RolPermiso permiso);
    Task<bool> SaveChangesAsync();
}
