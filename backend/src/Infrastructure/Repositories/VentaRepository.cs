using InventoryApp.Application.Common;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class VentaRepository : IVentaRepository
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public VentaRepository(AppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    private IQueryable<Venta> ConIncludes() =>
        _context.Ventas.Include(v => v.Detalles).ThenInclude(d => d.Producto)
            .Include(v => v.Factura)
            .Include(v => v.DescuentosAplicados)
            .Include(v => v.ImpuestosAplicados);

    private int? UsuarioAlcanceActual =>
        _currentUser.EstaAutenticado && !_currentUser.EsAdministrador
            ? _currentUser.UsuarioId
            : null;

    private static IQueryable<Venta> AplicarAlcance(IQueryable<Venta> query, int? usuarioId) =>
        usuarioId.HasValue ? query.Where(v => v.CreadoPorUsuarioId == usuarioId.Value) : query;

    public async Task<Venta?> GetByIdAsync(int id) =>
        await AplicarAlcance(ConIncludes(), UsuarioAlcanceActual)
            .FirstOrDefaultAsync(v => v.Id == id);

    public async Task<(List<Venta> Items, int TotalCount)> GetPagedAsync(PagedRequest request)
    {
        var scope = request.UsuarioIdScope ?? UsuarioAlcanceActual;
        var query = AplicarAlcance(ConIncludes().AsQueryable(), scope);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(v =>
                v.NumeroVenta.ToLower().Contains(search) ||
                v.ClienteNombre.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync();

        var sortDirDesc = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = (request.SortBy?.ToLower()) switch
        {
            "total" => sortDirDesc ? query.OrderByDescending(v => v.Total) : query.OrderBy(v => v.Total),
            "clientenombre" => sortDirDesc ? query.OrderByDescending(v => v.ClienteNombre) : query.OrderBy(v => v.ClienteNombre),
            _ => sortDirDesc ? query.OrderByDescending(v => v.Fecha) : query.OrderBy(v => v.Fecha),
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
        var query = AplicarAlcance(_context.Ventas.AsQueryable(), usuarioId ?? UsuarioAlcanceActual);
        return await query.CountAsync(v => v.Fecha >= inicioMes && v.Estado == EstadoDocumento.Confirmada);
    }

    public async Task<decimal> GetIngresosDelMesAsync(int? usuarioId = null)
    {
        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = AplicarAlcance(_context.Ventas.AsQueryable(), usuarioId ?? UsuarioAlcanceActual);
        return await query
            .Where(v => v.Fecha >= inicioMes && v.Estado == EstadoDocumento.Confirmada)
            .SumAsync(v => (decimal?)v.Total) ?? 0m;
    }

    public async Task<decimal> GetCuentasPorCobrarAsync(int? usuarioId = null)
    {
        var query = AplicarAlcance(_context.Ventas.AsQueryable(), usuarioId ?? UsuarioAlcanceActual);
        return await query
            .Where(v => v.Estado == EstadoDocumento.Confirmada && v.EstadoPago != EstadoPago.Pagado)
            .SumAsync(v => (decimal?)v.Total) ?? 0m;
    }

    public async Task<decimal> GetUtilidadBrutaTotalAsync(int? usuarioId = null)
    {
        var query = AplicarAlcance(_context.Ventas.AsQueryable(), usuarioId ?? UsuarioAlcanceActual);
        return await query
            .Where(v => v.Estado == EstadoDocumento.Confirmada)
            .SumAsync(v => (decimal?)v.UtilidadBruta) ?? 0m;
    }

    public async Task<List<Venta>> GetUltimasAsync(int cantidad = 5, int? usuarioId = null) =>
        await AplicarAlcance(ConIncludes(), usuarioId ?? UsuarioAlcanceActual)
            .OrderByDescending(v => v.Fecha)
            .Take(cantidad)
            .ToListAsync();

    public async Task<int> ContarTodasAsync() =>
        await _context.Ventas.IgnoreQueryFilters().CountAsync();

    public async Task AddAsync(Venta venta) =>
        await _context.Ventas.AddAsync(venta);

    public void Update(Venta venta) =>
        _context.Ventas.Update(venta);

    public async Task<bool> SaveChangesAsync()
    {
        var borradoresEliminados = _context.ChangeTracker.Entries<Venta>()
            .Where(e => e.State == EntityState.Modified &&
                        e.Entity.Estado == EstadoDocumento.Borrador &&
                        e.Entity.Detalles.Count == 0 &&
                        !e.Entity.Eliminado)
            .ToList();

        foreach (var entry in borradoresEliminados)
        {
            foreach (var detalleEntry in _context.ChangeTracker.Entries<VentaDetalle>()
                         .Where(d => d.State == EntityState.Deleted && d.Entity.VentaId == entry.Entity.Id))
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
        var nuevos = _context.ChangeTracker.Entries<VentaImpuesto>()
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
