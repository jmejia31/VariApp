using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

public class RolPermiso
{
    public int Id { get; set; }

    /// Enum legado, se conserva por compatibilidad durante la migración a roles dinámicos.
    public RolUsuario Rol { get; set; }

    /// FK al catálogo dinámico de roles. Nullable mientras conviven ambos modelos;
    /// se vuelve la fuente de verdad una vez migrados los datos existentes.
    public int? RolId { get; set; }

    public ModuloSistema Modulo { get; set; }
    public AccionPermiso Accion { get; set; }
    public bool Permitido { get; set; }
}
