using InventoryApp.Domain.Common;

namespace InventoryApp.Domain.Entities;

public class Producto : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Cantidad { get; set; }
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public string? ImagenUrl { get; set; }
    public string? ImagenPublicId { get; set; } // Cloudinary public_id, necesario para eliminar/reemplazar la imagen
    public int UmbralStockBajo { get; set; } = 5;

    public bool TieneStockBajo => Cantidad < UmbralStockBajo;
}
