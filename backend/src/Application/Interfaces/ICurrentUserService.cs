using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Interfaces;

public interface ICurrentUserService
{
    int? UsuarioId { get; }
    string? NombreUsuario { get; }
    string? NombreCompleto { get; }
    RolUsuario? Rol { get; }

    /// Id del rol dinámico (Domain.Entities.Rol) del usuario autenticado, si ya
    /// fue migrado al catálogo. Nullable durante la convivencia con el enum legado.
    int? RolId { get; }

    /// Verdadera fuente de "acceso total": viene del catálogo dinámico
    /// (Rol.EsAdministrador) con fallback al enum legado para JWTs viejos
    /// emitidos antes de esta fase.
    bool EsAdministrador { get; }

    bool EstaAutenticado { get; }
}
