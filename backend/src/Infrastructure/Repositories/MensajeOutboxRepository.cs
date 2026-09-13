using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

/// <summary>
/// Persistencia del outbox N7.1/N7.2. Mantiene el límite transaccional del contrato:
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

    public async Task<IReadOnlyList<MensajeOutbox>> ClaimDisponiblesAsync(
        int empresaId,
        DateTime ahoraUtc,
        int maximoMensajes,
        CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        if (ahoraUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe expresarse en UTC.", nameof(ahoraUtc));
        if (maximoMensajes is <= 0 or > 200)
            throw new ArgumentOutOfRangeException(nameof(maximoMensajes));

        // La lectura solo produce candidatos. La exclusividad se decide en el
        // UPDATE condicional: si otro worker ganó primero, affectedRows = 0 y
        // este worker no recibe ese mensaje.
        var candidatos = await Mensajes
            .AsNoTracking()
            .Where(mensaje =>
                mensaje.EmpresaId == empresaId &&
                (mensaje.Estado == EstadoMensajeOutbox.Pendiente ||
                 mensaje.Estado == EstadoMensajeOutbox.Fallido) &&
                mensaje.DisponibleDesdeUtc <= ahoraUtc &&
                mensaje.ProcesandoDesdeUtc == null)
            .OrderBy(mensaje => mensaje.DisponibleDesdeUtc)
            .ThenBy(mensaje => mensaje.Id)
            .Select(mensaje => mensaje.Id)
            .Take(maximoMensajes)
            .ToListAsync(cancellationToken);

        if (candidatos.Count == 0)
            return Array.Empty<MensajeOutbox>();

        var reclamados = new List<MensajeOutbox>(candidatos.Count);
        foreach (var id in candidatos)
        {
            var afectados = await Mensajes
                .Where(mensaje =>
                    mensaje.Id == id &&
                    mensaje.EmpresaId == empresaId &&
                    (mensaje.Estado == EstadoMensajeOutbox.Pendiente ||
                     mensaje.Estado == EstadoMensajeOutbox.Fallido) &&
                    mensaje.DisponibleDesdeUtc <= ahoraUtc &&
                    mensaje.ProcesandoDesdeUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(mensaje => mensaje.Estado, EstadoMensajeOutbox.Procesando)
                        .SetProperty(mensaje => mensaje.Intentos, mensaje => mensaje.Intentos + 1)
                        .SetProperty(mensaje => mensaje.ProcesandoDesdeUtc, ahoraUtc)
                        .SetProperty(mensaje => mensaje.UltimoIntentoEnUtc, ahoraUtc)
                        .SetProperty(mensaje => mensaje.UltimoError, (string?)null)
                        .SetProperty(mensaje => mensaje.FechaActualizacion, ahoraUtc),
                    cancellationToken);

            if (afectados != 1)
                continue;

            var reclamado = await Mensajes
                .AsNoTracking()
                .SingleAsync(
                    mensaje => mensaje.Id == id && mensaje.EmpresaId == empresaId,
                    cancellationToken);
            reclamados.Add(reclamado);
        }

        return reclamados;
    }
}
