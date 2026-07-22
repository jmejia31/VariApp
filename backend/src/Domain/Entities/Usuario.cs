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

    public int? RolId { get; set; }
    public Rol? RolEntidad { get; set; }

    public string? FotoPerfilUrl { get; set; }
    public string? FotoPerfilPublicId { get; set; }

    public bool Activo { get; set; } = true;
    public bool Bloqueado { get; set; }
    public string? MotivoBloqueo { get; set; }
    public DateTime? FechaBloqueo { get; set; }
    public int? BloqueadoPorUsuarioId { get; set; }

    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public int? CreadoPorUsuarioId { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }
    public DateTime? FechaActualizacion { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
