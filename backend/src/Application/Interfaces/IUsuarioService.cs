using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IUsuarioService
{
    Task<List<UsuarioDto>> GetAllAsync();
    Task<UsuarioDto> CreateAsync(CreateUsuarioDto dto);
    Task<UsuarioDto?> UpdateAsync(int id, UpdateUsuarioDto dto);
    Task<UsuarioDto?> UpdateEstadoAsync(int id, bool activo);
}
