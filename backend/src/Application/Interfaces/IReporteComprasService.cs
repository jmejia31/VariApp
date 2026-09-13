using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IReporteComprasService
{
    Task<PagedResult<ReporteComprasDetalleDto>> ObtenerDetallePaginadoAsync(ReporteComprasFiltroDto filtro, CancellationToken cancellationToken = default);
}
