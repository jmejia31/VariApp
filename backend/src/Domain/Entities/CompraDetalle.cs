namespace InventoryApp.Domain.Entities;

public class CompraDetalle
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public Compra? Compra { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int? ProductoVarianteId { get; set; }
    public ProductoVariante? ProductoVariante { get; set; }

    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public string ProductoNombreSnapshot { get; set; } = string.Empty;
    public string ProductoMarcaSnapshot { get; set; } = string.Empty;
    public string ProductoModeloSnapshot { get; set; } = string.Empty;
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoSkuSnapshot { get; set; }
}
