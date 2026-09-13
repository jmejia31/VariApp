using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

public interface IOutboxEffectDispatcher
{
    /// <summary>
    /// Ejecuta el efecto externo usando la identidad durable del mensaje. La
    /// implementación concreta debe propagar ClaveIdempotencia al proveedor.
    /// </summary>
    Task DispatchAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default);
}

public sealed record OutboxRetryProcessorOptions(
    int BatchSize = 50,
    int MaxAttempts = OutboxRetryPolicy.MaximoIntentosPredeterminado,
    TimeSpan? BaseDelay = null,
    TimeSpan? MaxDelay = null,
    TimeSpan? StaleClaimAfter = null,
    double JitterRatio = 0.20)
{
    public OutboxRetryProcessorOptions Validar()
    {
        var demoraBase = BaseDelay ?? OutboxRetryPolicy.DemoraBasePredeterminada;
        var demoraMaxima = MaxDelay ?? OutboxRetryPolicy.DemoraMaximaPredeterminada;
        var stale = StaleClaimAfter ?? TimeSpan.FromMinutes(10);

        if (BatchSize is <= 0 or > 200)
            throw new InvalidOperationException("Outbox:BatchSize debe estar entre 1 y 200.");
        if (MaxAttempts <= 0)
            throw new InvalidOperationException("Outbox:MaxAttempts debe ser mayor que cero.");
        if (demoraBase <= TimeSpan.Zero)
            throw new InvalidOperationException("Outbox:BaseDelay debe ser mayor que cero.");
        if (demoraMaxima < demoraBase)
            throw new InvalidOperationException("Outbox:MaxDelay no puede ser menor que BaseDelay.");
        if (stale <= TimeSpan.Zero)
            throw new InvalidOperationException("Outbox:StaleClaimAfter debe ser mayor que cero.");
        if (JitterRatio is < 0 or > 1)
            throw new InvalidOperationException("Outbox:JitterRatio debe estar entre 0 y 1.");

        return this with
        {
            BaseDelay = demoraBase,
            MaxDelay = demoraMaxima,
            StaleClaimAfter = stale
        };
    }
}

public sealed record OutboxRetryBatchResult(
    int StaleRecuperados,
    int Reclamados,
    int Entregados,
    int Reprogramados,
    int DeadLetter,
    int Supersedidos);

/// <summary>
/// Procesa un lote tenant-bound. No contiene scheduler/HostedService: la etapa
/// de ejecución programada puede invocarlo sin acoplar Application a un proveedor
/// externo. Cada transición queda protegida por empresa + id + intento esperado.
/// </summary>
public sealed class OutboxRetryProcessor
{
    private readonly IMensajeOutboxRepository _repository;
    private readonly OutboxRetryProcessorOptions _options;
    private readonly OutboxRetryPolicy _policy;
    private readonly Func<double> _jitterSample;

    public OutboxRetryProcessor(
        IMensajeOutboxRepository repository,
        OutboxRetryProcessorOptions options,
        Func<double>? jitterSample = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Validar();
        _policy = new OutboxRetryPolicy(
            _options.MaxAttempts,
            _options.BaseDelay,
            _options.MaxDelay,
            _options.JitterRatio);
        _jitterSample = jitterSample ?? Random.Shared.NextDouble;
    }

    public async Task<OutboxRetryBatchResult> ProcesarLoteAsync(
        int empresaId,
        DateTime ahoraUtc,
        IOutboxEffectDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        if (ahoraUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe expresarse en UTC.", nameof(ahoraUtc));
        ArgumentNullException.ThrowIfNull(dispatcher);
        cancellationToken.ThrowIfCancellationRequested();

        var staleAntes = ahoraUtc - _options.StaleClaimAfter!.Value;
        var staleRecuperados = await _repository.RecuperarProcesandoStaleAsync(
            empresaId,
            staleAntes,
            ahoraUtc,
            _options.BatchSize,
            cancellationToken);

        var mensajes = await _repository.ClaimDisponiblesAsync(
            empresaId,
            ahoraUtc,
            _options.BatchSize,
            cancellationToken);

        var entregados = 0;
        var reprogramados = 0;
        var deadLetter = 0;
        var supersedidos = 0;

        foreach (var mensaje in mensajes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await dispatcher.DispatchAsync(mensaje, cancellationToken);
                var confirmado = await _repository.MarcarEntregadoAsync(
                    empresaId,
                    mensaje.Id,
                    mensaje.Intentos,
                    ahoraUtc,
                    cancellationToken);
                if (confirmado)
                    entregados++;
                else
                    supersedidos++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // No persistimos Exception.Message, payload ni datos del proveedor.
                // La telemetría detallada pertenece a SEC_AUDIT con redacción.
                var decision = _policy.EvaluarDespuesDeFallo(
                    mensaje.Intentos,
                    ahoraUtc,
                    _jitterSample());
                var confirmado = await _repository.RegistrarFalloAsync(
                    empresaId,
                    mensaje.Id,
                    mensaje.Intentos,
                    "DELIVERY_FAILED",
                    ahoraUtc,
                    decision.DisponibleDesdeUtc ?? ahoraUtc,
                    decision.DeadLetter,
                    cancellationToken);

                if (!confirmado)
                {
                    supersedidos++;
                    continue;
                }

                if (decision.DeadLetter)
                    deadLetter++;
                else
                    reprogramados++;
            }
        }

        return new OutboxRetryBatchResult(
            staleRecuperados,
            mensajes.Count,
            entregados,
            reprogramados,
            deadLetter,
            supersedidos);
    }
}
