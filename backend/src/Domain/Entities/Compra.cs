using System.ComponentModel.DataAnnotations.Schema;
using InventoryApp.Domain.Common;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

public class Compra : ConfirmableEntity
{
    public string NumeroCompra { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public int? ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    public string ProveedorNombre { get; set; } = string.Empty;
    public string? ProveedorTelefono { get; set; }
    public string? ProveedorDocumento { get; set; }
    public string? DocumentoReferencia { get; set; }

    public EstadoDocumento Estado { get; set; } = EstadoDocumento.Borrador;
    public EstadoPago EstadoPago { get; set; } = EstadoPago.Pendiente;

    // Compatibilidad histórica ERP-N0. El enum se conserva hasta que N0.8.C/D
    // materialice y migre la relación; no debe ser la autoridad de operaciones nuevas.
    public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;

    // Contrato de dominio N0.8.B. La persistencia se habilita expresamente en N0.8.C
    // para evitar que esta microtarea genere DDL o una migración implícita.
    [NotMapped]
    public int? MetodoPagoId { get; set; }

    [NotMapped]
    public InventoryApp.Domain.Entities.Catalogos.MetodoPago? MetodoPagoCatalogo { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }

    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public string? Notas { get; set; }

    public ICollection<CompraDetalle> Detalles { get; set; } = new List<CompraDetalle>();
    public ICollection<CompraImpuesto> ImpuestosAplicados { get; set; } = new List<CompraImpuesto>();
}
