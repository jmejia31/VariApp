using InventoryApp.Domain.Common;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

/// <summary>
/// Punto físico/lógico de almacenamiento asociado a una Sucursal.
/// N1.2 define únicamente la topología maestra; existencias y ubicaciones
/// internas se incorporan en ERP-N1.3/N1.4.
/// El contexto de empresa futuro se deriva de Sucursal y no se duplica aquí
/// antes de que ERP-N6 defina la autoridad multiempresa.
/// </summary>
public class Almacen : AuditableEntity
{
    public int SucursalId { get; set; }
    public Sucursal Sucursal { get; set; } = null!;

    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public TipoAlmacen Tipo { get; set; }
    public bool Activo { get; set; } = true;

    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }
}
