using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _context;

    public ClienteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cliente?> GetByIdAsync(int id) =>
        await _context.Clientes.FindAsync(id);

    public async Task<Cliente?> GetByIdConVentasAsync(int id) =>
        await _context.Clientes.Include(c => c.Ventas).FirstOrDefaultAsync(c => c.Id == id);

    public async Task<List<Cliente>> GetAllAsync() =>
        await _context.Clientes.Include(c => c.Ventas).OrderBy(c => c.Nombre).ToListAsync();

    public async Task<List<Cliente>> GetActivosAsync() =>
        await _context.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync();

    public async Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null) =>
        await _context.Clientes.AnyAsync(c =>
            c.Nombre.ToLower() == nombre.ToLower() && (excluirId == null || c.Id != excluirId));

    public async Task AddAsync(Cliente cliente) =>
        await _context.Clientes.AddAsync(cliente);

    public void Update(Cliente cliente) =>
        _context.Clientes.Update(cliente);

    public void Remove(Cliente cliente) =>
        _context.Clientes.Remove(cliente);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
