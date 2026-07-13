using InventoryApp.Application.DTOs;
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

    public async Task<(List<RegistroAuditoria> Items, int TotalCount)> GetFilteredAsync(AuditoriaFiltroDto filtro)
    {
        var query = _context.RegistrosAuditoria.AsQueryable();

        if (filtro.UsuarioId.HasValue) query = query.Where(r => r.UsuarioId == filtro.UsuarioId.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Modulo)) query = query.Where(r => r.Modulo.ToString() == filtro.Modulo);
        if (!string.IsNullOrWhiteSpace(filtro.Accion)) query = query.Where(r => r.Accion.ToString() == filtro.Accion);
        if (!string.IsNullOrWhiteSpace(filtro.Entidad)) query = query.Where(r => r.Entidad == filtro.Entidad);
        if (filtro.ReferenciaId.HasValue) query = query.Where(r => r.ReferenciaId == filtro.ReferenciaId.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Resultado)) query = query.Where(r => r.Resultado == filtro.Resultado);
        if (filtro.Desde.HasValue) query = query.Where(r => r.Fecha >= filtro.Desde.Value);
        if (filtro.Hasta.HasValue) query = query.Where(r => r.Fecha <= filtro.Hasta.Value);

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();
            query = query.Where(r =>
                r.Descripcion.Contains(texto) ||
                r.NombreUsuario.Contains(texto) ||
                (r.Motivo != null && r.Motivo.Contains(texto)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.Fecha)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
