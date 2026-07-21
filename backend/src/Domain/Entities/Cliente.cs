using InventoryApp.Domain.Common;

namespace InventoryApp.Domain.Entities;

public class Cliente : AuditableEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? IdentidadORTN { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }
    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}
