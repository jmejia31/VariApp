using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IReporteInventarioService
{
    Task<PagedResult<ReporteInventarioReconciliacionDto>> ObtenerReporteReconciliacionAsync(
        ReporteInventarioReconciliacionFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReporteInventarioStockHealthDto>> ObtenerStockHealthAsync(
        ReporteInventarioStockHealthFiltroDto filtro,
        CancellationToken cancellationToken = default);
}
