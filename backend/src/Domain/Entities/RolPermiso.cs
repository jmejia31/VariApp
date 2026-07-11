using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

public class RolPermiso
{
    public int Id { get; set; }
    public RolUsuario Rol { get; set; }
    public ModuloSistema Modulo { get; set; }
    public AccionPermiso Accion { get; set; }
    public bool Permitido { get; set; }
}
