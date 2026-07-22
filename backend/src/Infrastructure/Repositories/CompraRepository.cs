using InventoryApp.Application.Common;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class CompraRepository : ICompraRepository
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CompraRepository(AppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    private IQueryable<Compra> ConIncludes() =>
        _context.Compras.Include(c => c.Detalles).ThenInclude(d => d.Producto)
            .Include(c => c.ImpuestosAplicados);

    private int? UsuarioAlcanceActual =>
        _currentUser.EstaAutenticado && !_currentUser.EsAdministrador
            ? _currentUser.UsuarioId
            : null;

    private static IQueryable<Compra> AplicarAlcance(IQueryable<Compra> query, int? usuarioId) =>
        usuarioId.HasValue ? query.Where(c => c.CreadoPorUsuarioId == usuarioId.Value) : query;

    public async Task<Compra?> GetByIdAsync(int id) =>
        await AplicarAlcance(ConIncludes(), UsuarioAlcanceActual)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<(List<Compra> Items, int TotalCount)> GetPagedAsync(PagedRequest request)
    {
        var scope = request.UsuarioIdScope ?? UsuarioAlcanceActual;
        var query = AplicarAlcance(ConIncludes().AsQueryable(), scope);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(c =>
                c.NumeroCompra.ToLower().Contains(search) ||
                c.ProveedorNombre.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync();

        var sortDirDesc = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = (request.SortBy?.ToLower()) switch
        {
            "total" => sortDirDesc ? query.OrderByDescending(c => c.Total) : query.OrderBy(c => c.Total),
            "proveedornombre" => sortDirDesc ? query.OrderByDescending(c => c.ProveedorNombre) : query.OrderBy(c => c.ProveedorNombre),
            _ => sortDirDesc ? query.OrderByDescending(c => c.Fecha) : query.OrderBy(c => c.Fecha),
        };

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();
        return (items, totalCount);
    }

    public async Task<int> GetTotalDelMesAsync(int? usuarioId = null)
    {
        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = AplicarAlcance(_context.Compras.AsQueryable(), usuarioId ?? UsuarioAlcanceActual);
        return await query.CountAsync(c => c.Fecha >= inicioMes && c.Estado == EstadoDocumento.Confirmada);
    }

    public async Task<decimal> GetCuentasPorPagarAsync(int? usuarioId = null)
    {
        var query = AplicarAlcance(_context.Compras.AsQueryable(), usuarioId ?? UsuarioAlcanceActual);
        return await query
            .Where(c => c.Estado == EstadoDocumento.Confirmada && c.EstadoPago != EstadoPago.Pagado)
            .SumAsync(c => (decimal?)c.Total) ?? 0m;
    }

    public async Task<List<Compra>> GetUltimasAsync(int cantidad = 5, int? usuarioId = null) =>
        await AplicarAlcance(ConIncludes(), usuarioId ?? UsuarioAlcanceActual)
            .OrderByDescending(c => c.Fecha)
            .Take(cantidad)
            .ToListAsync();

    public async Task<int> ContarTodasAsync() =>
        await _context.Compras.IgnoreQueryFilters().CountAsync();

    public async Task AddAsync(Compra compra) =>
        await _context.Compras.AddAsync(compra);

    public void Update(Compra compra) =>
        _context.Compras.Update(compra);

    public async Task<bool> SaveChangesAsync()
    {
        var borradoresEliminados = _context.ChangeTracker.Entries<Compra>()
            .Where(e => e.State == EntityState.Modified &&
                        e.Entity.Estado == EstadoDocumento.Borrador &&
                        e.Entity.Detalles.Count == 0 &&
                        !e.Entity.Eliminado)
            .ToList();

        foreach (var entry in borradoresEliminados)
        {
            foreach (var detalleEntry in _context.ChangeTracker.Entries<CompraDetalle>()
                         .Where(d => d.State == EntityState.Deleted && d.Entity.CompraId == entry.Entity.Id))
            {
                detalleEntry.State = EntityState.Unchanged;
            }

            entry.Entity.Eliminado = true;
            entry.Entity.FechaEliminacion = DateTime.UtcNow;
            entry.Entity.EliminadoPorUsuarioId = _currentUser.UsuarioId;
            entry.Entity.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
            entry.Entity.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
            entry.Entity.FechaActualizacion = DateTime.UtcNow;
        }

        await CompletarSnapshotImpuestosAsync();
        return await _context.SaveChangesAsync() > 0;
    }

    private async Task CompletarSnapshotImpuestosAsync()
    {
        var nuevos = _context.ChangeTracker.Entries<CompraImpuesto>()
            .Where(e => e.State == EntityState.Added)
            .ToList();
        if (nuevos.Count == 0) return;

        var ids = nuevos.Select(e => e.Entity.ImpuestoId).Distinct().ToList();
        var configuracion = await _context.Impuestos.AsNoTracking()
            .Where(i => ids.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.IncluidoEnPrecio);

        foreach (var entry in nuevos)
        {
            entry.Entity.IncluidoEnPrecioSnapshot =
                configuracion.TryGetValue(entry.Entity.ImpuestoId, out var incluido) && incluido;
        }
    }
}
