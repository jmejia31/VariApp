using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Interfaces;

public interface ICurrentUserService
{
    int? UsuarioId { get; }
    string? NombreUsuario { get; }
    string? NombreCompleto { get; }
    RolUsuario? Rol { get; }
    bool EstaAutenticado { get; }
}
