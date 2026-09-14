using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public sealed class PagoOnlineRepository : IPagoOnlineRepository
{
    private readonly AppDbContext _db;

    public PagoOnlineRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Factura?> ObtenerFacturaAsync(int facturaId, CancellationToken cancellationToken = default) =>
        _db.Facturas
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == facturaId, cancellationToken);

    public Task<PagoOnline?> ObtenerPorIdAsync(
        int empresaId,
        int id,
        CancellationToken cancellationToken = default) =>
        _db.Set<PagoOnline>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

    public Task<PagoOnline?> ObtenerPorIdempotenciaAsync(
        int empresaId,
        string proveedor,
        string claveIdempotenciaHash,
        CancellationToken cancellationToken = default) =>
        _db.Set<PagoOnline>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.EmpresaId == empresaId &&
                     x.Proveedor == proveedor &&
                     x.ClaveIdempotenciaHash == claveIdempotenciaHash,
                cancellationToken);

    public async Task<(IReadOnlyList<PagoOnline> Items, int Total)> ListarAsync(
        int empresaId,
        int pagina,
        int tamanoPagina,
        EstadoPagoOnline? estado,
        int? facturaId,
        string? proveedor,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Set<PagoOnline>()
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId);

        if (estado.HasValue)
            query = query.Where(x => x.Estado == estado.Value);

        if (facturaId.HasValue)
            query = query.Where(x => x.FacturaId == facturaId.Value);

        if (!string.IsNullOrWhiteSpace(proveedor))
            query = query.Where(x => x.Proveedor == proveedor);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreadoUtc)
            .ThenByDescending(x => x.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AgregarAsync(PagoOnline pago, CancellationToken cancellationToken = default) =>
        await _db.Set<PagoOnline>().AddAsync(pago, cancellationToken);

    public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
