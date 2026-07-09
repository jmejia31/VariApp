using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;

    public UsuarioRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario) =>
        await _context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);

    public async Task<Usuario?> GetByIdAsync(int id) =>
        await _context.Usuarios.FindAsync(id);

    public async Task<List<Usuario>> GetAllAsync() =>
        await _context.Usuarios.OrderBy(u => u.NombreUsuario).ToListAsync();

    public async Task AddAsync(Usuario usuario) =>
        await _context.Usuarios.AddAsync(usuario);

    public void Update(Usuario usuario) =>
        _context.Usuarios.Update(usuario);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
