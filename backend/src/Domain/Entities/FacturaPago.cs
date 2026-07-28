using InventoryApp.Domain.Common;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

public class FacturaPago : AuditableEntity
{
    public int FacturaId { get; set; }
    public Factura? Factura { get; set; }

    public DateTime FechaPago { get; set; } = DateTime.UtcNow;
    public decimal Monto { get; set; }
    public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;
    public string? Referencia { get; set; }
    public string? Observaciones { get; set; }

    public bool Anulado { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public int? AnuladoPorUsuarioId { get; set; }
    public string? AnuladoPorNombreUsuario { get; set; }
    public string? MotivoAnulacion { get; set; }
}
