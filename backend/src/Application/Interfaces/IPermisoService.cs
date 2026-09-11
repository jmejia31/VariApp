using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Interfaces;

public interface IPermisoService
{
    /// Matriz completa del rol indicado (todas las combinaciones Módulo/Acción
    /// válidas según CatalogoPermisosBase, con Permitido según lo guardado).
    Task<List<PermisoMatrizItemDto>> GetMatrizAsync(int rolId);
    Task<List<PermisoMatrizItemDto>> UpdateMatrizAsync(int rolId, UpdatePermisoMatrizDto dto);

    /// Siembra los permisos por defecto de un rol recién creado, solo si el rol
    /// no tiene ninguna fila en su matriz todavía (idempotente, sección 8/9).
    Task PrecargarMatrizPorDefectoAsync(int rolId, bool esAdministrador);

    Task<MisPermisosDto> GetMisPermisosAsync();
    Task<bool> TienePermisoAsync(ModuloSistema modulo, AccionPermiso accion);
    Task VerificarPermisoAsync(ModuloSistema modulo, AccionPermiso accion);
}
