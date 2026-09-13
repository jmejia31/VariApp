using InventoryApp.Application.Common;
using InventoryApp.Domain.Entities.Contabilidad;

namespace InventoryApp.Application.DTOs.Contabilidad;

public sealed class PeriodoContableQueryDto : PagedRequest
{
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public EstadoPeriodoContable? Estado { get; set; }
}
