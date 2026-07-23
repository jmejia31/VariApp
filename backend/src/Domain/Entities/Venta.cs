using InventoryApp.Domain.Common;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

public class Venta : ConfirmableEntity
{
    public string NumeroVenta { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public string ClienteNombre { get; set; } = "Cliente final";
    public string? ClienteTelefono { get; set; }
    public string? ClienteIdentidadORTN { get; set; }
    public string? ClienteCorreo { get; set; }
    public string? ClienteDireccion { get; set; }

    public EstadoDocumento Estado { get; set; } = EstadoDocumento.Borrador;
    public EstadoPago EstadoPago { get; set; } = EstadoPago.Pendiente;
    public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;

    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public decimal CostoTotal { get; set; }
    public decimal UtilidadBruta { get; set; }

    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public string? Notas { get; set; }

    public ICollection<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();
    public ICollection<VentaDescuento> DescuentosAplicados { get; set; } = new List<VentaDescuento>();
    public ICollection<VentaImpuesto> ImpuestosAplicados { get; set; } = new List<VentaImpuesto>();
    public Factura? Factura { get; set; }
}
