using InventoryApp.Domain.Common;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

public class Compra : ConfirmableEntity
{
    public string NumeroCompra { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    /// Referencia al proveedor registrado (opcional: permite compras rápidas sin
    /// crear un proveedor formal). Los campos de abajo son snapshot del proveedor
    /// al momento de la compra, para no alterar el historial si el proveedor cambia después.
    public int? ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    public string ProveedorNombre { get; set; } = string.Empty;
    public string? ProveedorTelefono { get; set; }
    public string? ProveedorDocumento { get; set; }
    public string? DocumentoReferencia { get; set; }

    public EstadoDocumento Estado { get; set; } = EstadoDocumento.Borrador;
    public EstadoPago EstadoPago { get; set; } = EstadoPago.Pendiente;
    public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;

    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }

    public string? Notas { get; set; }

    public ICollection<CompraDetalle> Detalles { get; set; } = new List<CompraDetalle>();
}
