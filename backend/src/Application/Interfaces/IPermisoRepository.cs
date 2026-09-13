using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

/// Repositorio del catálogo de permisos (Domain.Entities.Permiso). Antes de esta
/// fase existía como código muerto (no registrado en DI, sin llamadores),
/// operando sobre RolPermiso; se reutiliza y redefine aquí para el catálogo real
/// en vez de crear un repositorio paralelo.
public interface IPermisoRepository
{
    Task<Permiso?> GetByIdAsync(int id);
    Task<Permiso?> GetByModuloAccionAsync(InventoryApp.Domain.Enums.ModuloSistema modulo, InventoryApp.Domain.Enums.AccionPermiso accion);
    Task<List<Permiso>> GetAllAsync(bool incluirEliminados = false);
    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null);
    Task<bool> ExisteModuloAccionAsync(InventoryApp.Domain.Enums.ModuloSistema modulo, InventoryApp.Domain.Enums.AccionPermiso accion, int? excluirId = null);
    Task<int> ContarAsignacionesAsync(int permisoId);
    Task AddAsync(Permiso permiso);
    void Update(Permiso permiso);
    void Remove(Permiso permiso);
    Task SaveChangesAsync();
}
