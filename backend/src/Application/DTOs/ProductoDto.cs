namespace InventoryApp.Application.DTOs;

public class ProductoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Cantidad { get; set; }
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public string? ImagenUrl { get; set; }
    public int UmbralStockBajo { get; set; }
    public bool TieneStockBajo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}
