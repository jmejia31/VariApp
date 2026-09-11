using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Security;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Services;

/// <summary>
/// Resuelve el contexto tenant efectivo exclusivamente desde persistencia.
/// Los claims sólo identifican al usuario autenticado; nunca autorizan una Empresa.
/// </summary>
public sealed class ContextoTenantService : IContextoTenantService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ContextoTenantService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ContextoTenantActual?> ResolverAsync(
        int empresaId,
        CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0 || !_currentUser.EstaAutenticado || !_currentUser.UsuarioId.HasValue)
            return null;

        var usuarioId = _currentUser.UsuarioId.Value;

        var usuarioValido = await _db.Usuarios
            .AsNoTracking()
            .AnyAsync(
                u => u.Id == usuarioId && u.Activo && !u.Bloqueado && !u.Eliminado,
                cancellationToken);
        if (!usuarioValido)
            return null;

        var empresaValida = await _db.Empresas
            .AsNoTracking()
            .AnyAsync(e => e.Id == empresaId && e.Activa, cancellationToken);
        if (!empresaValida)
            return null;

        var membresia = await _db.UsuarioEmpresas
            .AsNoTracking()
            .SingleOrDefaultAsync(
                m => m.UsuarioId == usuarioId && m.EmpresaId == empresaId && m.Activa,
                cancellationToken);
        if (membresia is null)
            return null;

        var rolValido = await _db.Roles
            .AsNoTracking()
            .AnyAsync(
                r => r.Id == membresia.RolId && r.Activo && !r.Eliminado,
                cancellationToken);
        if (!rolValido)
            return null;

        return ContextoTenantActual.DesdeMembresia(membresia, usuarioId, empresaId);
    }
}
