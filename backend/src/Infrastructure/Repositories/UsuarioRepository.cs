using InventoryApp.Application.Common;
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
        await _context.Usuarios.Include(u => u.RolEntidad)
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario && !u.Eliminado);

    public async Task<Usuario?> GetByIdAsync(int id) =>
        await _context.Usuarios.Include(u => u.RolEntidad)
            .FirstOrDefaultAsync(u => u.Id == id && !u.Eliminado);

    public async Task<List<Usuario>> GetAllAsync() =>
        await _context.Usuarios.Include(u => u.RolEntidad)
            .Where(u => !u.Eliminado)
            .OrderBy(u => u.NombreUsuario).ToListAsync();

    public async Task<PagedResult<Usuario>> GetPagedAsync(PagedRequest request)
    {
        var query = _context.Usuarios.Include(u => u.RolEntidad).Where(u => !u.Eliminado);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var termino = request.Search.Trim();
            query = query.Where(u =>
                u.NombreUsuario.Contains(termino) ||
                u.NombreCompleto.Contains(termino));
        }

        query = (request.SortBy, request.SortDirection?.ToLower()) switch
        {
            ("NombreUsuario", "desc") => query.OrderByDescending(u => u.NombreUsuario),
            ("NombreUsuario", _) => query.OrderBy(u => u.NombreUsuario),
            ("FechaCreacion", "desc") => query.OrderByDescending(u => u.FechaCreacion),
            ("FechaCreacion", _) => query.OrderBy(u => u.FechaCreacion),
            (_, "desc") => query.OrderByDescending(u => u.NombreCompleto),
            _ => query.OrderBy(u => u.NombreCompleto)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<Usuario>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    public async Task<int> ContarAdministradoresActivosAsync(int? excluirUsuarioId = null) =>
        await _context.Usuarios
            .Include(u => u.RolEntidad)
            .CountAsync(u =>
                !u.Eliminado && u.Activo && !u.Bloqueado &&
                ((u.RolEntidad != null && u.RolEntidad.EsAdministrador) ||
                 (u.RolEntidad == null && u.Rol == Domain.Enums.RolUsuario.Administrador)) &&
                (excluirUsuarioId == null || u.Id != excluirUsuarioId));

    public async Task AddAsync(Usuario usuario) =>
        await _context.Usuarios.AddAsync(usuario);

    public void Update(Usuario usuario) =>
        _context.Usuarios.Update(usuario);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
