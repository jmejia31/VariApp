using InventoryApp.Domain.Common;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

/// <summary>
/// Ubicación física/lógica dentro de un Almacén. ERP-N1.3 modela únicamente
/// topología interna; las existencias por ubicación pertenecen a ERP-N1.4.
/// El contexto de Sucursal/Empresa se deriva del Almacén y no se duplica aquí.
/// </summary>
public class UbicacionAlmacen : AuditableEntity
{
    public int AlmacenId { get; set; }
    public Almacen Almacen { get; set; } = null!;

    public int? UbicacionPadreId { get; set; }
    public UbicacionAlmacen? UbicacionPadre { get; set; }
    public ICollection<UbicacionAlmacen> Hijas { get; set; } = new List<UbicacionAlmacen>();

    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public TipoUbicacionAlmacen Tipo { get; set; }
    public bool Activa { get; set; } = true;

    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }
}
