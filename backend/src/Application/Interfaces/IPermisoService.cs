using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Interfaces;

public interface IPermisoService
{
    Task<List<PermisoMatrizItemDto>> GetMatrizAsync();
    Task<List<PermisoMatrizItemDto>> UpdateMatrizAsync(UpdatePermisoMatrizDto dto);
    Task<MisPermisosDto> GetMisPermisosAsync();
    Task<bool> TienePermisoAsync(ModuloSistema modulo, AccionPermiso accion);
    Task VerificarPermisoAsync(ModuloSistema modulo, AccionPermiso accion);
}
