using InventoryApp.Application.Common;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.DTOs;

public class CotizacionFiltroDto : PagedRequest
{
    public int? ClienteId { get; set; }
    public EstadoCotizacion? Estado { get; set; }
    public DateTime? FechaDesdeUtc { get; set; }
    public DateTime? FechaHastaUtc { get; set; }
}
