using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface ISolicitudCompraService
{
    Task<PagedResult<SolicitudCompraDto>> GetPagedAsync(SolicitudCompraFiltroDto filtro);
    Task<SolicitudCompraDto?> GetByIdAsync(int id);
    Task<SolicitudCompraDto> CreateAsync(CreateSolicitudCompraDto dto);
    Task<SolicitudCompraDto> UpdateAsync(int id, UpdateSolicitudCompraDto dto);
    Task<SolicitudCompraDto> EnviarAsync(int id);
    Task<SolicitudCompraDto> AprobarAsync(int id);
    Task<SolicitudCompraDto> RechazarAsync(int id, RechazarSolicitudCompraDto dto);
}
