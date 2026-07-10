using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

public class Factura
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public Venta? Venta { get; set; }

    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
    public EstadoFactura Estado { get; set; } = EstadoFactura.Emitida;

    public string EmpresaNombre { get; set; } = string.Empty;
    public string? EmpresaRTN { get; set; }
    public string? EmpresaTelefono { get; set; }
    public string? EmpresaCorreo { get; set; }
    public string? EmpresaDireccion { get; set; }

    public string ClienteNombre { get; set; } = string.Empty;
    public string? ClienteTelefono { get; set; }
    public string? ClienteIdentidadORTN { get; set; }
    public string? ClienteCorreo { get; set; }
    public string? ClienteDireccion { get; set; }

    public int VendedorUsuarioId { get; set; }
    public string VendedorNombreUsuario { get; set; } = string.Empty;
    public int? GeneradaPorUsuarioId { get; set; }
    public string? GeneradaPorNombreUsuario { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }

    public string? Observaciones { get; set; }

    public DateTime? FechaAnulacion { get; set; }
    public int? AnuladaPorUsuarioId { get; set; }
    public string? AnuladaPorNombreUsuario { get; set; }
    public string? MotivoAnulacion { get; set; }

    public ICollection<FacturaDetalle> Detalles { get; set; } = new List<FacturaDetalle>();
}
