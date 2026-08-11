using System.ComponentModel.DataAnnotations.Schema;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Fuente de verdad del rol del usuario desde ERP-N0.4.
    /// </summary>
    public int RolId { get; set; }
    public Rol RolEntidad { get; set; } = null!;

    /// <summary>
    /// Compatibilidad binaria/fixtures antiguos. No se persiste y no puede usarse
    /// para autorizar. El runtime de seguridad resuelve siempre RolId/RolEntidad.
    /// </summary>
    [NotMapped]
    [Obsolete("ERP-N0.4: RolUsuario es compatibilidad no persistida; use RolId/RolEntidad.")]
    public RolUsuario Rol { get; set; } = RolUsuario.Vendedor;

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
