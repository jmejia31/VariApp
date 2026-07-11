using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Interfaces;

public interface IRolPermisoRepository
{
    Task<List<RolPermiso>> GetAllAsync();
    Task<List<RolPermiso>> GetByRolAsync(RolUsuario rol);
    Task<bool> TienePermisoAsync(RolUsuario rol, ModuloSistema modulo, AccionPermiso accion);
    Task ReemplazarMatrizAsync(List<RolPermiso> nuevaMatriz);
}
