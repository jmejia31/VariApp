using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class AuditoriaRepository : IAuditoriaRepository
{
    private readonly AppDbContext _context;

    public AuditoriaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RegistroAuditoria registro) =>
        await _context.RegistrosAuditoria.AddAsync(registro);

    public async Task<(List<RegistroAuditoria> Items, int TotalCount)> GetFilteredAsync(
        int? usuarioId, string? modulo, string? accion, DateTime? desde, DateTime? hasta, int page, int pageSize)
    {
        var query = _context.RegistrosAuditoria.AsQueryable();

        if (usuarioId.HasValue) query = query.Where(r => r.UsuarioId == usuarioId.Value);
        if (!string.IsNullOrWhiteSpace(modulo)) query = query.Where(r => r.Modulo.ToString() == modulo);
        if (!string.IsNullOrWhiteSpace(accion)) query = query.Where(r => r.Accion.ToString() == accion);
        if (desde.HasValue) query = query.Where(r => r.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(r => r.Fecha <= hasta.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
