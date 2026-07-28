namespace InventoryApp.Application.DTOs;

public class ProductoVarianteDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public int ColorId { get; set; }
    public string ColorNombre { get; set; } = string.Empty;
    public string? ColorCodigoVisual { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public int Cantidad { get; set; }
    public int UmbralStockBajo { get; set; }
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public bool Activo { get; set; }
    public bool TieneStockBajo { get; set; }
    public bool EstaAgotada { get; set; }
    public string EstadoInventario { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public class CreateProductoVarianteDto
{
    public int ColorId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public int Cantidad { get; set; }
    public int UmbralStockBajo { get; set; } = 5;
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
}

public class UpdateProductoVarianteDto : CreateProductoVarianteDto
{
}

public class CambiarEstadoProductoVarianteDto
{
    public bool Activo { get; set; }
}
