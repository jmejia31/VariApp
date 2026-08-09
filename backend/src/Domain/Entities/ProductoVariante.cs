using InventoryApp.Domain.Common;

namespace InventoryApp.Domain.Entities;

public class ProductoVariante : AuditableEntity
{
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int? MarcaId { get; set; }
    public Marca? Marca { get; set; }

    public int? ModeloId { get; set; }
    public Modelo? Modelo { get; set; }

    public int? ColorId { get; set; }
    public Color? Color { get; set; }

    public int? TallaId { get; set; }
    public Talla? Talla { get; set; }

    public string? Sku { get; set; }
    public string? CodigoBarras { get; set; }
    public int Cantidad { get; set; }
    public int UmbralStockBajo { get; set; } = 5;
    public decimal? Costo { get; set; }
    public decimal? Precio { get; set; }
    public bool EsTecnica { get; set; }
    public bool Activo { get; set; } = true;
    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public ICollection<ProductoImagen> Imagenes { get; set; } = new List<ProductoImagen>();

    public bool TieneStockBajo => Activo && !Eliminado && Cantidad > 0 && Cantidad < UmbralStockBajo;
    public bool EstaAgotada => Activo && !Eliminado && Cantidad <= 0;
}
