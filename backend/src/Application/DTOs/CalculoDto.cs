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
    public decimal Tasa { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal Monto { get; set; }
    public bool IncluidoEnPrecio { get; set; }
}

public class ResultadoCalculoDto
{
    public decimal Subtotal { get; set; }
    public List<DescuentoAplicadoDto> DescuentosAplicados { get; set; } = new();
    public decimal TotalDescuento { get; set; }
    public List<ImpuestoAplicadoDto> ImpuestosAplicados { get; set; } = new();
    public decimal TotalImpuesto { get; set; }
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
