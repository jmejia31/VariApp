using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

/// <summary>
/// Persistencia del outbox N7.1. Mantiene el límite transaccional del contrato:
/// AddAsync agrega la intención al DbContext, pero no confirma la unidad de trabajo.
/// </summary>
public sealed class MensajeOutboxRepository : IMensajeOutboxRepository
{
    private readonly AppDbContext _context;

    public MensajeOutboxRepository(AppDbContext context)
    {
        _context = context;
    }

    private DbSet<MensajeOutbox> Mensajes => _context.Set<MensajeOutbox>();

    public async Task AddAsync(
        MensajeOutbox mensaje,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mensaje);
        await Mensajes.AddAsync(mensaje, cancellationToken);
    }

    public Task<MensajeOutbox?> GetByEventoIdAsync(
        int empresaId,
        Guid eventoId,
        CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaId));

        return Mensajes
            .AsNoTracking()
            .SingleOrDefaultAsync(
                mensaje => mensaje.EmpresaId == empresaId && mensaje.EventoId == eventoId,
                cancellationToken);
    }

    public Task<bool> ExisteClaveIdempotenciaAsync(
        int empresaId,
        string claveIdempotencia,
        CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        if (string.IsNullOrWhiteSpace(claveIdempotencia))
            throw new ArgumentException("La clave de idempotencia es obligatoria.", nameof(claveIdempotencia));

        var normalizada = claveIdempotencia.Trim();
        return Mensajes
            .AsNoTracking()
            .AnyAsync(
                mensaje => mensaje.EmpresaId == empresaId && mensaje.ClaveIdempotencia == normalizada,
                cancellationToken);
    }
}
