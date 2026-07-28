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

    // Compatibilidad durante la migración hacia inventario por variantes.
    // Cuando existan variantes, la cantidad consolidada se mantiene como la
    // suma física de todas las variantes no eliminadas, incluso si alguna está
    // temporalmente inactiva para nuevas operaciones.
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

    // Color heredado: se conserva para retrocompatibilidad y para crear la
    // variante inicial de productos existentes. La fuente futura será Variantes.
    public int? ColorId { get; set; }
    public CatalogoProducto? Color { get; set; }
    public int? TallaId { get; set; }
    public CatalogoProducto? Talla { get; set; }
    public int? MarcaId { get; set; }
    public CatalogoProducto? MarcaCatalogo { get; set; }
    public int? ModeloId { get; set; }
    public CatalogoProducto? ModeloCatalogo { get; set; }

    public ICollection<ProductoImagen> Imagenes { get; set; } = new List<ProductoImagen>();
    public ICollection<ProductoVariante> Variantes { get; set; } = new List<ProductoVariante>();

    public int CantidadVariantesActivas => Variantes
        .Where(v => v.Activo && !v.Eliminado)
        .Sum(v => v.Cantidad);

    public bool TieneStockBajo => Activo && !Eliminado && Cantidad > 0 && Cantidad < UmbralStockBajo;
    public bool EstaAgotado => Activo && !Eliminado && Cantidad <= 0;

    // Compatibilidad: imagen principal calculada a partir de la colección.
    public ProductoImagen? ImagenPrincipal =>
        Imagenes.Where(i => i.EsPrincipal).FirstOrDefault() ?? Imagenes.OrderBy(i => i.Orden).FirstOrDefault();
}
