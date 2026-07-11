using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class RolPermisoRepository : IRolPermisoRepository
{
    private readonly AppDbContext _context;

    public RolPermisoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RolPermiso>> GetAllAsync() =>
        await _context.RolPermisos.ToListAsync();

    public async Task<List<RolPermiso>> GetByRolAsync(RolUsuario rol) =>
        await _context.RolPermisos.Where(p => p.Rol == rol).ToListAsync();

    public async Task<bool> TienePermisoAsync(RolUsuario rol, ModuloSistema modulo, AccionPermiso accion)
    {
        if (rol == RolUsuario.Administrador) return true;

        var permiso = await _context.RolPermisos
            .FirstOrDefaultAsync(p => p.Rol == rol && p.Modulo == modulo && p.Accion == accion);

        return permiso?.Permitido ?? false;
    }

    public async Task ReemplazarMatrizAsync(List<RolPermiso> nuevaMatriz)
    {
        var actuales = await _context.RolPermisos.Where(p => p.Rol == RolUsuario.Vendedor).ToListAsync();
        _context.RolPermisos.RemoveRange(actuales);
        await _context.RolPermisos.AddRangeAsync(nuevaMatriz.Where(p => p.Rol == RolUsuario.Vendedor));
        await _context.SaveChangesAsync();
    }
}
