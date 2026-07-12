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

    public async Task<List<RolPermiso>> GetByRolIdAsync(int rolId) =>
        await _context.RolPermisos.Where(p => p.RolId == rolId).ToListAsync();

    public async Task<bool> TieneMatrizDefinidaAsync(int rolId) =>
        await _context.RolPermisos.AnyAsync(p => p.RolId == rolId);

    public async Task<bool> TienePermisoPorRolIdAsync(int rolId, ModuloSistema modulo, AccionPermiso accion)
    {
        var permiso = await _context.RolPermisos
            .FirstOrDefaultAsync(p => p.RolId == rolId && p.Modulo == modulo && p.Accion == accion);
        return permiso?.Permitido ?? false;
    }

    public async Task ReemplazarMatrizPorRolIdAsync(int rolId, List<RolPermiso> nuevaMatriz)
    {
        var actuales = await _context.RolPermisos.Where(p => p.RolId == rolId).ToListAsync();
        _context.RolPermisos.RemoveRange(actuales);
        await _context.RolPermisos.AddRangeAsync(nuevaMatriz.Where(p => p.RolId == rolId));
        await _context.SaveChangesAsync();
    }

    public async Task AgregarSiFaltaAsync(List<RolPermiso> filas)
    {
        // Idempotente: solo agrega filas para combinaciones RolId+Modulo+Accion que
        // aún no existan. Nunca sobrescribe una fila ya presente (respeta cambios
        // manuales del administrador, sección 8/9).
        foreach (var fila in filas)
        {
            var existe = await _context.RolPermisos.AnyAsync(p =>
                p.RolId == fila.RolId && p.Modulo == fila.Modulo && p.Accion == fila.Accion);
            if (!existe)
                await _context.RolPermisos.AddAsync(fila);
        }
        await _context.SaveChangesAsync();
    }
}
