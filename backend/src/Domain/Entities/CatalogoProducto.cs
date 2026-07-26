using InventoryApp.Domain.Common;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

/// <summary>
/// Catálogo reutilizable de atributos de producto. Los modelos utilizan
/// CatalogoPadreId para relacionarse con una marca.
/// </summary>
public class CatalogoProducto : AuditableEntity
{
    public TipoCatalogoProducto Tipo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? CodigoVisual { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public int? CatalogoPadreId { get; set; }
    public CatalogoProducto? CatalogoPadre { get; set; }
    public ICollection<CatalogoProducto> ElementosHijos { get; set; } = new List<CatalogoProducto>();

    public ICollection<Producto> ProductosComoColor { get; set; } = new List<Producto>();
    public ICollection<Producto> ProductosComoTalla { get; set; } = new List<Producto>();
    public ICollection<Producto> ProductosComoMarca { get; set; } = new List<Producto>();
    public ICollection<Producto> ProductosComoModelo { get; set; } = new List<Producto>();
}
