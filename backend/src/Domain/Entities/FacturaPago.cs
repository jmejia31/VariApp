using InventoryApp.Domain.Common;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

public class FacturaPago : AuditableEntity
{
    public int FacturaId { get; set; }
    public Factura? Factura { get; set; }

    public DateTime FechaPago { get; set; } = DateTime.UtcNow;
    public decimal Monto { get; set; }

    // Compatibilidad ERP-N0: el valor enum se conserva hasta completar el backfill.
    public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;
    public int? MetodoPagoId { get; set; }
    public InventoryApp.Domain.Entities.Catalogos.MetodoPago? MetodoPagoCatalogo { get; set; }

    public string? Referencia { get; set; }
    public string? Observaciones { get; set; }

    public bool Anulado { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public int? AnuladoPorUsuarioId { get; set; }
    public string? AnuladoPorNombreUsuario { get; set; }
    public string? MotivoAnulacion { get; set; }
}
