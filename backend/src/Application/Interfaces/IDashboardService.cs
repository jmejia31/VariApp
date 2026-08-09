using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardResumenDto> GetResumenAsync();
    Task<InventarioVariantesReporteDto> GetInventarioVariantesAsync(
        int? productoId = null,
        int? marcaId = null,
        int? modeloId = null,
        int? colorId = null,
        int? tallaId = null,
        bool incluirInactivas = true,
        CancellationToken cancellationToken = default);
}
