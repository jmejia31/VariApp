namespace InventoryApp.Domain.Entities;

public class VentaDetalle
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public Venta? Venta { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal CostoUnitarioSnapshot { get; set; }
    public decimal Subtotal { get; set; }
    public decimal UtilidadBruta { get; set; }

    public string ProductoNombreSnapshot { get; set; } = string.Empty;
    public string ProductoMarcaSnapshot { get; set; } = string.Empty;
    public string ProductoModeloSnapshot { get; set; } = string.Empty;
}
