namespace InventoryApp.Application.DTOs;

public class FacturaDetalleDto
{
    public string ProductoNombre { get; set; } = string.Empty;
    public string ProductoMarca { get; set; } = string.Empty;
    public string ProductoModelo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Subtotal { get; set; }
}

public class FacturaDto
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public string NumeroVentaOrigen { get; set; } = string.Empty;
    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public string Estado { get; set; } = string.Empty;

    public string EmpresaNombre { get; set; } = string.Empty;
    public string? EmpresaRTN { get; set; }
    public string? EmpresaTelefono { get; set; }
    public string? EmpresaCorreo { get; set; }
    public string? EmpresaDireccion { get; set; }
    public string? EmpresaEslogan { get; set; }
    public string? EmpresaTextoFactura { get; set; }
    public string? EmpresaTextoLegal { get; set; }
    public string? EmpresaCopyright { get; set; }
    /// Logo ACTUAL de la empresa (no snapshot histórico como el resto de
    /// datos de empresa): se resuelve en vivo al generar el PDF, a
    /// diferencia del nombre/RTN/teléfono que sí quedan fijos al emitir la
    /// factura. Decisión documentada: el logo es identidad visual vigente,
    /// no un dato legal que deba congelarse en el tiempo.
    public string? EmpresaLogoUrl { get; set; }

    public string ClienteNombre { get; set; } = string.Empty;
    public string? ClienteTelefono { get; set; }
    public string? ClienteIdentidadORTN { get; set; }
    public string? ClienteCorreo { get; set; }
    public string? ClienteDireccion { get; set; }

    public string VendedorNombreUsuario { get; set; } = string.Empty;
    public string? GeneradaPorNombreUsuario { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public string EstadoPago { get; set; } = string.Empty;

    public string? Observaciones { get; set; }
    public List<FacturaDetalleDto> Detalles { get; set; } = new();
    public List<DescuentoAplicadoDto> DescuentosAplicados { get; set; } = new();
    public List<ImpuestoAplicadoDto> ImpuestosAplicados { get; set; } = new();

    public DateTime? FechaAnulacion { get; set; }
    public string? AnuladaPorNombreUsuario { get; set; }
    public string? MotivoAnulacion { get; set; }
}
