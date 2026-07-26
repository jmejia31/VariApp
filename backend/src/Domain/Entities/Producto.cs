using InventoryApp.Domain.Common;

namespace InventoryApp.Domain.Entities;

public class Producto : AuditableEntity
{
    public string Nombre { get; set; } = string.Empty;

    // Campos de compatibilidad y snapshot histórico. La fuente administrable
    // pasa a ser MarcaId/ModeloId, pero se conservan para datos anteriores.
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

    public int? ColorId { get; set; }
    public CatalogoProducto? Color { get; set; }
    public int? TallaId { get; set; }
    public CatalogoProducto? Talla { get; set; }
    public int? MarcaId { get; set; }
    public CatalogoProducto? MarcaCatalogo { get; set; }
    public int? ModeloId { get; set; }
    public CatalogoProducto? ModeloCatalogo { get; set; }

    public ICollection<ProductoImagen> Imagenes { get; set; } = new List<ProductoImagen>();

    public bool TieneStockBajo => Activo && !Eliminado && Cantidad > 0 && Cantidad < UmbralStockBajo;
    public bool EstaAgotado => Activo && !Eliminado && Cantidad <= 0;

    // Compatibilidad: imagen principal calculada a partir de la colección.
    public ProductoImagen? ImagenPrincipal =>
        Imagenes.Where(i => i.EsPrincipal).FirstOrDefault() ?? Imagenes.OrderBy(i => i.Orden).FirstOrDefault();
}
