using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IVentaService
{
    Task<VentaDto?> GetByIdAsync(int id);
    Task<PagedResult<VentaDto>> GetPagedAsync(PagedRequest request);
    Task<VentaDto> CreateAsync(CreateVentaDto dto);
    Task<VentaDto?> UpdateAsync(int id, UpdateVentaDto dto);
    Task<VentaDto?> ConfirmarAsync(int id);
    Task<VentaDto?> AnularAsync(int id, string motivo);
    Task<bool> DeleteBorradorAsync(int id);
}
