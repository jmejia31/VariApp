using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IUsuarioService
{
    Task<List<UsuarioDto>> GetAllAsync();
    Task<PagedResult<UsuarioDto>> GetPagedAsync(PagedRequest request);
    Task<UsuarioDetalleDto?> GetByIdAsync(int id);
    Task<UsuarioDto> CreateAsync(CreateUsuarioDto dto);
    Task<UsuarioDto?> UpdateAsync(int id, UpdateUsuarioDto dto);
    Task<UsuarioDto?> UpdateEstadoAsync(int id, bool activo);
    Task<UsuarioDto> BloquearAsync(int id, string motivo);
    Task<UsuarioDto> DesbloquearAsync(int id);
    Task EliminarAsync(int id);
}
