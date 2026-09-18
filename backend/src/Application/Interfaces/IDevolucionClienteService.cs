using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IDevolucionClienteService
{
    Task<PagedResult<DevolucionClienteDto>> GetPagedAsync(DevolucionClienteFiltroDto filtro);
    Task<DevolucionClienteDto> GetByIdAsync(int id);
    Task<DevolucionClienteDto> CrearAsync(CreateDevolucionClienteDto dto, string idempotencyKey);
    Task<DevolucionClienteDto> ConfirmarAsync(int id);
    Task<DevolucionClienteDto> AnularAsync(int id, string motivo);
}
