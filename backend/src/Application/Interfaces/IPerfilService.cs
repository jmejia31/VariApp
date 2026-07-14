using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

/// Autogestión del propio usuario autenticado: ver/editar su nombre, cambiar
/// su propia contraseña. Deliberadamente separado de IUsuarioService (que
/// administra a OTROS usuarios y requiere permisos de Usuarios) — aquí solo
/// se requiere estar autenticado, cada quien gestiona su propia cuenta.
public interface IPerfilService
{
    Task<PerfilDto> GetPerfilAsync();
    Task<PerfilDto> ActualizarPerfilAsync(ActualizarPerfilDto dto);
    Task CambiarPasswordAsync(CambiarPasswordPropiaDto dto);
}
