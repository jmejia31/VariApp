namespace InventoryApp.Application.DTOs;

public class DetalleCalculoInput
{
    public int ProductoId { get; set; }
    public int? CategoriaId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class DescuentoAplicadoDto
{
    public int DescuentoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public decimal Monto { get; set; }
}

public class ImpuestoAplicadoDto
{
    public int ImpuestoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public decimal Tasa { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal Monto { get; set; }
    public bool IncluidoEnPrecio { get; set; }
}

public class ResultadoCalculoDto
{
    /// Suma de cantidad por precio unitario antes de descuentos e impuestos.
    public decimal ImporteBruto { get; set; }

    /// Base neta sin impuestos incluidos, después de aplicar descuentos.
    /// Este es el subtotal contable que debe persistirse en ventas, compras y facturas.
    public decimal Subtotal { get; set; }

    /// Alias explícito para las interfaces nuevas.
    public decimal SubtotalNeto
    {
        get => Subtotal;
        set => Subtotal = value;
    }

    public List<DescuentoAplicadoDto> DescuentosAplicados { get; set; } = new();
    public decimal TotalDescuento { get; set; }
    public List<ImpuestoAplicadoDto> ImpuestosAplicados { get; set; } = new();
    public decimal TotalImpuesto { get; set; }
    public decimal ImpuestoIncluido { get; set; }
    public decimal ImpuestoAdicional { get; set; }

    /// Total final: subtotal neto + impuesto incluido + impuesto adicional.
    public decimal Total { get; set; }
}

public class CalcularVentaRequest
{
    public int? ClienteId { get; set; }
    public string? CodigoPromocional { get; set; }
    public List<VentaDetalleInputDto> Detalles { get; set; } = new();
}

public class CalcularCompraRequest
{
    public int? ProveedorId { get; set; }
    public List<VentaDetalleInputDto> Detalles { get; set; } = new();
}
