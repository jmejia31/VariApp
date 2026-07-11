using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IProveedorService
{
    Task<List<ProveedorDto>> GetAllAsync();
    Task<List<ProveedorDto>> GetActivosAsync();
    Task<List<ProveedorDto>> BuscarActivosAsync(string termino);
    Task<ProveedorDto?> GetByIdAsync(int id);
    Task<ProveedorDto> CreateAsync(CreateProveedorDto dto);
    Task<ProveedorDto?> UpdateAsync(int id, UpdateProveedorDto dto);
    Task<bool> DeleteAsync(int id);
}
