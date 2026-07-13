namespace InventoryApp.Application.DTOs;

public class CompraDetalleDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string ProductoMarca { get; set; } = string.Empty;
    public string ProductoModelo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class CompraDto
{
    public int Id { get; set; }
    public string NumeroCompra { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int? ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public string? ProveedorTelefono { get; set; }
    public string? ProveedorDocumento { get; set; }
    public string? DocumentoReferencia { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string EstadoPago { get; set; } = string.Empty;
    public string MetodoPago { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public string? Notas { get; set; }
    public List<CompraDetalleDto> Detalles { get; set; } = new();
    public List<ImpuestoAplicadoDto> ImpuestosAplicados { get; set; } = new();

    public string? CreadoPorNombreUsuario { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? ConfirmadoPorNombreUsuario { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public string? AnuladoPorNombreUsuario { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public string? MotivoAnulacion { get; set; }
}

public class CompraDetalleInputDto
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
}

public class CreateCompraDto
{
    public int? ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public string? ProveedorTelefono { get; set; }
    public string? ProveedorDocumento { get; set; }
    public string? DocumentoReferencia { get; set; }
    public string MetodoPago { get; set; } = "Efectivo";
    public string EstadoPago { get; set; } = "Pendiente";

    /// OBSOLETO / IGNORADO POR EL BACKEND (sección 13). Se conserva en el DTO
    /// solo para no romper clientes viejos; el backend siempre recalcula
    /// Impuesto desde el catálogo real vía ICalculoService. Descuento en
    /// compras no está implementado todavía (ver limitación documentada en
    /// CompraService.CalcularTotalesAsync).
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public string? Notas { get; set; }
    public List<CompraDetalleInputDto> Detalles { get; set; } = new();
}

public class UpdateCompraDto : CreateCompraDto
{
}

public class AnularDocumentoDto
{
    public string MotivoAnulacion { get; set; } = string.Empty;
}
