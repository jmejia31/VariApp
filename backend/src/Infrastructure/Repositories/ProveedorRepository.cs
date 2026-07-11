using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class ProveedorRepository : IProveedorRepository
{
    private readonly AppDbContext _context;

    public ProveedorRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Proveedor?> GetByIdAsync(int id) =>
        await _context.Proveedores.FindAsync(id);

    public async Task<Proveedor?> GetByIdConComprasAsync(int id) =>
        await _context.Proveedores.Include(p => p.Compras).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<Proveedor>> GetAllAsync() =>
        await _context.Proveedores.Include(p => p.Compras).OrderBy(p => p.Nombre).ToListAsync();

    public async Task<List<Proveedor>> GetActivosAsync() =>
        await _context.Proveedores.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();

    public async Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null) =>
        await _context.Proveedores.AnyAsync(p =>
            p.Nombre.ToLower() == nombre.ToLower() && (excluirId == null || p.Id != excluirId));

    public async Task AddAsync(Proveedor proveedor) =>
        await _context.Proveedores.AddAsync(proveedor);

    public void Update(Proveedor proveedor) =>
        _context.Proveedores.Update(proveedor);

    public void Remove(Proveedor proveedor) =>
        _context.Proveedores.Remove(proveedor);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
