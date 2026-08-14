using InventoryApp.Domain.Common;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

/// <summary>
/// Punto físico/lógico de almacenamiento asociado a una Sucursal.
/// N1.2 define únicamente la topología maestra; existencias y ubicaciones
/// internas se incorporan en ERP-N1.3/N1.4.
/// </summary>
public class Almacen : AuditableEntity
{
    /// <summary>
    /// Identificador futuro de empresa/tenant. Permanece nullable hasta que ERP-N6
    /// introduzca la entidad raíz y su aislamiento; N1 no crea una FK ficticia.
    /// Debe mantenerse coherente con la EmpresaId futura de la Sucursal asociada.
    /// </summary>
    public int? EmpresaId { get; set; }

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
