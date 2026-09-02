using InventoryApp.Application.DTOs.Bancos;

namespace InventoryApp.Application.Interfaces;

public interface ICuentaBancariaService
{
    Task<CuentaBancariaDto?> GetByIdAsync(int id);
    Task<List<CuentaBancariaDto>> GetAllAsync();
    Task<List<CuentaBancariaDto>> GetActivasAsync();
    Task<CuentaBancariaDto> AddAsync(CreateCuentaBancariaDto dto);
    Task ActivarAsync(int id);
    Task DesactivarAsync(int id);
}
