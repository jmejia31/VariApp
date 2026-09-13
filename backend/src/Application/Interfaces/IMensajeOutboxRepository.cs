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

    /// <summary>
    /// Libera claims abandonados antes del corte indicado. La implementación
    /// debe volver a Fallido únicamente filas que continúen en Procesando y cuyo
    /// ProcesandoDesdeUtc siga siendo stale al ejecutar el update condicional.
    /// </summary>
    Task<int> RecuperarProcesandoStaleAsync(
        int empresaId,
        DateTime staleAntesUtc,
        DateTime ahoraUtc,
        int maximoMensajes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirma entrega únicamente si el intento esperado sigue siendo el owner
    /// lógico del claim. Devuelve false si el claim fue recuperado/supersedido.
    /// </summary>
    Task<bool> MarcarEntregadoAsync(
        int empresaId,
        int mensajeId,
        int intentoEsperado,
        DateTime ahoraUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirma un fallo del intento esperado y programa la próxima ventana o
    /// DeadLetter. Nunca debe aceptar la transición de un claim stale/supersedido.
    /// </summary>
    Task<bool> RegistrarFalloAsync(
        int empresaId,
        int mensajeId,
        int intentoEsperado,
        string errorSeguro,
        DateTime ahoraUtc,
        DateTime disponibleDesdeUtc,
        bool deadLetter,
        CancellationToken cancellationToken = default);
}
