namespace InventoryApp.Domain.Entities;

public class DevolucionClienteDetalle
{
    public int Id { get; set; }
    public int DevolucionClienteId { get; set; }
    public DevolucionCliente? DevolucionCliente { get; set; }

    public int VentaDetalleId { get; set; }
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitarioSnapshot { get; set; }
    public string ProductoNombreSnapshot { get; set; } = string.Empty;
    public string? ProductoSkuSnapshot { get; set; }
    public string? Motivo { get; set; }

    public decimal Subtotal => decimal.Round(Cantidad * PrecioUnitarioSnapshot, 4, MidpointRounding.AwayFromZero);

    public void Validar()
    {
        if (VentaDetalleId <= 0)
            throw new InvalidOperationException("El detalle de venta origen es obligatorio.");
        if (ProductoId <= 0)
            throw new InvalidOperationException("El producto es obligatorio.");
        if (Cantidad <= 0)
            throw new InvalidOperationException("La cantidad devuelta debe ser mayor que cero.");
        if (PrecioUnitarioSnapshot < 0)
            throw new InvalidOperationException("El precio unitario snapshot no puede ser negativo.");
        if (string.IsNullOrWhiteSpace(ProductoNombreSnapshot))
            throw new InvalidOperationException("El snapshot del nombre del producto es obligatorio.");
    }
}
