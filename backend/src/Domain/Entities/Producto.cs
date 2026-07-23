using InventoryApp.Domain.Common;

namespace InventoryApp.Domain.Entities;

public class Producto : AuditableEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Cantidad { get; set; }
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public int UmbralStockBajo { get; set; } = 5;

    public bool Activo { get; set; } = true;
    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public int? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public ICollection<ProductoImagen> Imagenes { get; set; } = new List<ProductoImagen>();

    public bool TieneStockBajo => Activo && !Eliminado && Cantidad < UmbralStockBajo;

    // Compatibilidad: imagen principal calculada a partir de la colección.
    public ProductoImagen? ImagenPrincipal =>
        Imagenes.Where(i => i.EsPrincipal).FirstOrDefault() ?? Imagenes.OrderBy(i => i.Orden).FirstOrDefault();
}
