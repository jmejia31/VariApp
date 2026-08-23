using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IEvaluacionProveedorService
{
    Task<PagedResult<EvaluacionProveedorDto>> GetPagedAsync(EvaluacionProveedorFiltroDto filtro);
    Task<EvaluacionProveedorDto?> GetByIdAsync(int id);
    Task<EvaluacionProveedorDto> GenerarPorRecepcionAsync(int recepcionCompraId);
}
