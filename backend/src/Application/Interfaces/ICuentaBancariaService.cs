using InventoryApp.Application.DTOs.Bancos;
using InventoryApp.Application.Bancos;

namespace InventoryApp.Application.Interfaces;

public interface ICuentaBancariaService
{
    Task<CuentaBancariaDto?> GetByIdAsync(int id);
    Task<CuentaBancariaPage<CuentaBancariaDto>> GetAllAsync(CuentaBancariaQueryFilter filter);
    Task<List<CuentaBancariaDto>> GetActivasAsync();
    Task<CuentaBancariaDto> AddAsync(CreateCuentaBancariaDto dto);
    Task ActivarAsync(int id);
    Task DesactivarAsync(int id);
    Task UpdateAsync(int id, UpdateCuentaBancariaDto dto);
}
