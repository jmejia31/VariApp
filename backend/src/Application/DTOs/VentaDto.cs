namespace InventoryApp.Application.DTOs;

public class VentaDetalleDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string ProductoMarca { get; set; } = string.Empty;
    public string ProductoModelo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public decimal UtilidadBruta { get; set; }
}

public class VentaDto
{
    public int Id { get; set; }
    public string NumeroVenta { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int? ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string? ClienteTelefono { get; set; }
    public string? ClienteIdentidadORTN { get; set; }
    public string? ClienteCorreo { get; set; }
    public string? ClienteDireccion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string EstadoPago { get; set; } = string.Empty;
    public string MetodoPago { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public decimal CostoTotal { get; set; }
    public decimal UtilidadBruta { get; set; }
    public string? Notas { get; set; }
    public List<VentaDetalleDto> Detalles { get; set; } = new();

    public int? FacturaId { get; set; }
    public string? NumeroFactura { get; set; }

    public string? CreadoPorNombreUsuario { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? ConfirmadoPorNombreUsuario { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public string? AnuladoPorNombreUsuario { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public string? MotivoAnulacion { get; set; }
}

public class VentaDetalleInputDto
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class CreateVentaDto
{
    public int? ClienteId { get; set; }
    public string ClienteNombre { get; set; } = "Cliente final";
    public string? ClienteTelefono { get; set; }
    public string? ClienteIdentidadORTN { get; set; }
    public string? ClienteCorreo { get; set; }
    public string? ClienteDireccion { get; set; }
    public string MetodoPago { get; set; } = "Efectivo";
    public string EstadoPago { get; set; } = "Pendiente";
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public string? Notas { get; set; }
    public List<VentaDetalleInputDto> Detalles { get; set; } = new();
}

public class UpdateVentaDto : CreateVentaDto
{
}
