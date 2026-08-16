using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public sealed class ConteoInventarioRepository : IConteoInventarioRepository
{
    private readonly AppDbContext _context;

    public ConteoInventarioRepository(AppDbContext context) => _context = context;

    private DbSet<ConteoInventario> Conteos => _context.Set<ConteoInventario>();

    private IQueryable<ConteoInventario> ConDetalle(bool tracking = false)
    {
        var query = tracking ? Conteos.AsTracking() : Conteos.AsNoTracking();
        return query
            .Include(x => x.Almacen)
            .Include(x => x.UbicacionAlmacen)
            .Include(x => x.Categoria)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.ProductoVariante)
            .AsSplitQuery();
    }

    public async Task<(List<ConteoInventario> Items, int TotalCount)> GetPagedAsync(ConteoInventarioFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        IQueryable<ConteoInventario> query = Conteos.AsNoTracking();

        if (filtro.Estado.HasValue) query = query.Where(x => x.Estado == filtro.Estado.Value);
        if (filtro.Tipo.HasValue) query = query.Where(x => x.Tipo == filtro.Tipo.Value);
        if (filtro.AlmacenId.HasValue) query = query.Where(x => x.AlmacenId == filtro.AlmacenId.Value);
        if (filtro.UbicacionAlmacenId.HasValue) query = query.Where(x => x.UbicacionAlmacenId == filtro.UbicacionAlmacenId.Value);
        if (filtro.CategoriaId.HasValue) query = query.Where(x => x.CategoriaId == filtro.CategoriaId.Value);
        if (filtro.EsCiego.HasValue) query = query.Where(x => x.EsCiego == filtro.EsCiego.Value);
        if (filtro.Desde.HasValue) query = query.Where(x => x.FechaCreacion >= filtro.Desde.Value);
        if (filtro.Hasta.HasValue) query = query.Where(x => x.FechaCreacion <= filtro.Hasta.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Numero))
        {
            var numero = filtro.Numero.Trim();
            query = query.Where(x => x.Numero.Contains(numero));
        }

        var totalCount = await query.CountAsync();
        var desc = string.Equals(filtro.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = filtro.SortBy?.Trim().ToLowerInvariant() switch
        {
            "numero" => desc ? query.OrderByDescending(x => x.Numero).ThenByDescending(x => x.Id) : query.OrderBy(x => x.Numero).ThenBy(x => x.Id),
            "estado" => desc ? query.OrderByDescending(x => x.Estado).ThenByDescending(x => x.Id) : query.OrderBy(x => x.Estado).ThenBy(x => x.Id),
            "tipo" => desc ? query.OrderByDescending(x => x.Tipo).ThenByDescending(x => x.Id) : query.OrderBy(x => x.Tipo).ThenBy(x => x.Id),
            _ => desc ? query.OrderByDescending(x => x.FechaCreacion).ThenByDescending(x => x.Id) : query.OrderBy(x => x.FechaCreacion).ThenBy(x => x.Id)
        };

        var items = await query
            .Include(x => x.Almacen)
            .Include(x => x.UbicacionAlmacen)
            .Include(x => x.Categoria)
            .Include(x => x.Detalles).ThenInclude(x => x.ProductoVariante)
            .AsSplitQuery()
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync();
        return (items, totalCount);
    }

    public Task<ConteoInventario?> GetByIdAsync(int id) =>
        ConDetalle().FirstOrDefaultAsync(x => x.Id == id);

    public async Task<ConteoInventario?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        var conteo = await Conteos
            .FromSqlInterpolated($"SELECT c.* FROM ConteosInventario c WHERE c.Id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();
        if (conteo is null) return null;

        await _context.Entry(conteo).Reference(x => x.Almacen).LoadAsync();
        await _context.Entry(conteo).Reference(x => x.UbicacionAlmacen).LoadAsync();
        await _context.Entry(conteo).Reference(x => x.Categoria).LoadAsync();
        await _context.Entry(conteo).Collection(x => x.Detalles).Query()
            .Include(x => x.ProductoVariante)
            .LoadAsync();
        return conteo;
    }

    public Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null)
    {
        var normalizado = numero.Trim();
        return Conteos.AsNoTracking().AnyAsync(x => x.Numero == normalizado && (!excluirId.HasValue || x.Id != excluirId.Value));
    }

    public Task AddAsync(ConteoInventario conteo) => Conteos.AddAsync(conteo).AsTask();
    public void Update(ConteoInventario conteo) => Conteos.Update(conteo);
    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
