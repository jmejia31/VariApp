using InventoryApp.Domain.Common;

namespace InventoryApp.Domain.Entities;

public class Categoria : AuditableEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activa { get; set; } = true;

    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
