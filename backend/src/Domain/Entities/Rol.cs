namespace InventoryApp.Domain.Entities;

/// Catálogo dinámico de roles (reemplaza el enum estático RolUsuario como fuente
/// de verdad). RolUsuario se conserva únicamente para compatibilidad de JWTs
/// existentes durante la migración; Rol.Id es la referencia real desde Usuario.
public class Rol
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string NombreNormalizado { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsSistema { get; set; }
    public bool EsAdministrador { get; set; }
    public bool Activo { get; set; } = true;
    public bool Eliminado { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
    public DateTime? FechaEliminacion { get; set; }

    public int? CreadoPorUsuarioId { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    public ICollection<RolPermiso> Permisos { get; set; } = new List<RolPermiso>();
}
