using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    /// Enum legado, se conserva por compatibilidad durante la migración a roles dinámicos.
    public RolUsuario Rol { get; set; } = RolUsuario.Vendedor;

    /// FK al catálogo dinámico de roles (Domain.Entities.Rol). Nullable mientras
    /// conviven ambos modelos; los datos existentes se backfillean en la migración.
    public int? RolId { get; set; }
    public Rol? RolEntidad { get; set; }

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
