using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IClienteService
{
    Task<List<ClienteDto>> GetAllAsync();
    Task<List<ClienteDto>> GetActivosAsync();
    Task<List<ClienteDto>> BuscarActivosAsync(string termino);
    Task<ClienteDto?> GetByIdAsync(int id);
    Task<ClienteDto> CreateAsync(CreateClienteDto dto);
    Task<ClienteDto?> UpdateAsync(int id, UpdateClienteDto dto);
    Task<bool> DeleteAsync(int id);
}
