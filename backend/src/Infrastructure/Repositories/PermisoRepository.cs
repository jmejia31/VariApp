using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class PermisoRepository : IPermisoRepository
{
    private readonly AppDbContext _context;

    public PermisoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RolPermiso>> GetAllAsync() =>
        await _context.RolPermisos.ToListAsync();

    public async Task<RolPermiso?> GetAsync(RolUsuario rol, string modulo) =>
        await _context.RolPermisos.FirstOrDefaultAsync(p => p.Rol == rol && p.Modulo.ToString() == modulo);

    public async Task AddAsync(RolPermiso permiso) =>
        await _context.RolPermisos.AddAsync(permiso);

    public void Update(RolPermiso permiso) =>
        _context.RolPermisos.Update(permiso);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
