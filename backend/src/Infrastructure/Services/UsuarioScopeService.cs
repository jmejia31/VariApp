using InventoryApp.Application.Interfaces;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Services;

/// <summary>
/// Resuelve en cada solicitud el usuario y rol vigentes desde MySQL. Esto evita
/// que un JWT emitido antes de un cambio de rol conserve privilegios obsoletos y
/// garantiza que el alcance de datos se aplique por Usuario.Id.
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
                u.RolId.HasValue &&
                u.RolEntidad != null &&
                u.RolEntidad.Activo &&
                !u.RolEntidad.Eliminado)
            .Select(u => new UsuarioScopeActual(
                u.Id,
                u.RolId!.Value,
                u.RolEntidad!.Nombre,
                u.RolEntidad.EsAdministrador))
            .SingleOrDefaultAsync();
    }
}
