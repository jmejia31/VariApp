using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IRentabilidadVentasService
{
    Task<IReadOnlyList<ReporteRentabilidadDto>> ObtenerAsync(
        ReporteVentasFiltroDto filtro,
        RentabilidadAgrupacion agrupacion,
        CancellationToken cancellationToken = default);
}
