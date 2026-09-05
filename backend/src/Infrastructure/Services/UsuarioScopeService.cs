using InventoryApp.Application.Interfaces;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Services;

/// <summary>
/// Resuelve en cada solicitud el usuario y rol vigentes desde MySQL. El JWT solo
/// identifica la sesión; los privilegios efectivos se determinan con la relación
/// Usuario.RolId y los grants RolPermiso.
/// </summary>
public sealed class UsuarioScopeService : IUsuarioScopeService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UsuarioScopeService(AppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<UsuarioScopeActual?> ObtenerActualAsync()
    {
        if (!_currentUser.EstaAutenticado || !_currentUser.UsuarioId.HasValue)
            return null;

        var usuarioId = _currentUser.UsuarioId.Value;
        return await _context.Usuarios
            .AsNoTracking()
            .Where(u =>
                u.Id == usuarioId &&
                !u.Eliminado &&
                u.Activo &&
                !u.Bloqueado &&
                u.RolId > 0 &&
                u.RolEntidad.Activo &&
                !u.RolEntidad.Eliminado)
            .Select(u => new UsuarioScopeActual(
                u.Id,
                u.RolId,
                u.RolEntidad.Nombre,
                u.RolEntidad.EsAdministrador))
            .SingleOrDefaultAsync();
    }
}
