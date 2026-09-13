using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

/// <summary>
/// Contrato de persistencia del outbox. AddAsync NO confirma transacción por sí
/// mismo: el llamador debe persistir el mensaje en la misma unidad de trabajo
/// que la mutación de negocio que originó el efecto externo.
/// </summary>
public interface IMensajeOutboxRepository
{
    Task AddAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default);

    Task<MensajeOutbox?> GetByEventoIdAsync(
        int empresaId,
        Guid eventoId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteClaveIdempotenciaAsync(
        int empresaId,
        string claveIdempotencia,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reclama de forma atómica hasta <paramref name="maximoMensajes"/> mensajes
    /// vencidos del tenant. Dos workers concurrentes no pueden obtener el mismo
    /// mensaje: la implementación debe usar compare-and-set/lock equivalente.
    /// </summary>
    Task<IReadOnlyList<MensajeOutbox>> ClaimDisponiblesAsync(
        int empresaId,
        DateTime ahoraUtc,
        int maximoMensajes,
        CancellationToken cancellationToken = default);
}
