using System.Security.Claims;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace InventoryApp.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool EstaAutenticado => User?.Identity?.IsAuthenticated ?? false;

    public int? UsuarioId
    {
        get
        {
            var value = User?.FindFirstValue("id");
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public string? NombreUsuario => User?.FindFirstValue("nombreUsuario");

    public string? NombreCompleto => User?.FindFirstValue("nombreCompleto");

    public RolUsuario? Rol
    {
        get
        {
            var value = User?.FindFirstValue("rol");
            return Enum.TryParse<RolUsuario>(value, out var rol) ? rol : null;
        }
    }

    public int? RolId
    {
        get
        {
            var value = User?.FindFirstValue("rolId");
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public bool EsAdministrador
    {
        get
        {
            var value = User?.FindFirstValue("esAdministrador");
            if (bool.TryParse(value, out var esAdmin)) return esAdmin;
            // Fallback para JWTs emitidos antes de esta fase (sin el claim nuevo).
            return Rol == RolUsuario.Administrador;
        }
    }
}
