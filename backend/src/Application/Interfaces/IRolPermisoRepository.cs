using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Interfaces;

public interface IRolPermisoRepository
{
    Task<List<RolPermiso>> GetAllAsync();
    Task<List<RolPermiso>> GetByRolAsync(RolUsuario rol);
    Task<bool> TienePermisoAsync(RolUsuario rol, ModuloSistema modulo, AccionPermiso accion);
    Task ReemplazarMatrizAsync(List<RolPermiso> nuevaMatriz);

    // --- Catálogo dinámico de roles (RolId) ---
    Task<List<RolPermiso>> GetByRolIdAsync(int rolId);
    Task<bool> TieneMatrizDefinidaAsync(int rolId);
    Task<bool> TienePermisoPorRolIdAsync(int rolId, ModuloSistema modulo, AccionPermiso accion);
    Task ReemplazarMatrizPorRolIdAsync(int rolId, List<RolPermiso> nuevaMatriz);
    Task AgregarSiFaltaAsync(List<RolPermiso> filas);
}
