using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

/// Autogestión del propio usuario autenticado. Está separada de la
/// administración de terceros y no depende de permisos de módulo.
public interface IPerfilService
{
    Task<PerfilDto> GetPerfilAsync();
    Task<PerfilDto> ActualizarPerfilAsync(ActualizarPerfilDto dto);
    Task<PerfilDto> ActualizarFotoAsync(ActualizarFotoPerfilDto dto);
    Task<PerfilDto> EliminarFotoAsync();
    Task CambiarPasswordAsync(CambiarPasswordPropiaDto dto);
}
