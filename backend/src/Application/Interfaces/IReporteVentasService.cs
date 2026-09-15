using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryApp.Application.Interfaces;

public interface IReporteVentasService
{
    Task<ReporteVentasResumenDto> ObtenerResumenAsync(ReporteVentasFiltroDto filtro, CancellationToken cancellationToken = default);
    Task<PagedResult<ReporteVentasDetalleDto>> ObtenerDetallePaginadoAsync(ReporteVentasFiltroDto filtro, CancellationToken cancellationToken = default);
}
