using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IConsumoInsumoService
{
    Task<List<ConsumoInsumoDto>> GetAllAsync();
    Task<ConsumoInsumoDto?> GetByIdAsync(int id);
    Task<ConsumoInsumoDto> CreateAsync(CreateConsumoInsumoDto dto);
    Task<ConsumoInsumoDto?> UpdateAsync(int id, UpdateConsumoInsumoDto dto);
    Task<bool> DeleteBorradorAsync(int id);
}
