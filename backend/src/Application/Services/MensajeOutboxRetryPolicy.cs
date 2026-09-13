namespace InventoryApp.Application.Services;

/// <summary>
/// Configuración fail-closed para reintentos del outbox. Los valores se validan
/// al construir la política; un valor inválido impide iniciar el procesador.
/// </summary>
public sealed record MensajeOutboxRetryOptions(
    int MaxAttempts,
    TimeSpan InitialDelay,
    TimeSpan MaximumDelay,
    int DeterministicJitterPercent = 0)
{
    public void Validate()
    {
        if (MaxAttempts is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(MaxAttempts), "MaxAttempts debe estar entre 1 y 100.");

        if (InitialDelay <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(InitialDelay), "InitialDelay debe ser mayor que cero.");

        if (MaximumDelay < InitialDelay || MaximumDelay > TimeSpan.FromDays(7))
            throw new ArgumentOutOfRangeException(
                nameof(MaximumDelay),
                "MaximumDelay debe ser mayor o igual que InitialDelay y no exceder siete días.");

        if (DeterministicJitterPercent is < 0 or > 25)
            throw new ArgumentOutOfRangeException(
                nameof(DeterministicJitterPercent),
                "DeterministicJitterPercent debe estar entre 0 y 25.");
    }
}

/// <summary>
/// Calcula backoff exponencial, acotado y reproducible. No muta el mensaje ni
/// decide el claim: esas responsabilidades permanecen en el procesador y el
/// repositorio transaccional.
/// </summary>
public sealed class MensajeOutboxRetryPolicy
{
    private readonly MensajeOutboxRetryOptions _options;

    public MensajeOutboxRetryPolicy(MensajeOutboxRetryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _options = options;
    }

    public int MaxAttempts => _options.MaxAttempts;

    public bool CanRetry(int completedAttempts)
    {
        if (completedAttempts < 0)
            throw new ArgumentOutOfRangeException(nameof(completedAttempts));

        return completedAttempts < _options.MaxAttempts;
    }

    public TimeSpan CalculateDelay(int completedAttempts, Guid eventId)
    {
        if (completedAttempts <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(completedAttempts),
                "Debe existir al menos un intento completado.");

        if (eventId == Guid.Empty)
            throw new ArgumentException("EventoId es obligatorio para jitter determinista.", nameof(eventId));

        if (!CanRetry(completedAttempts))
            throw new InvalidOperationException("El mensaje agotó el máximo de intentos.");

        var exponent = Math.Min(completedAttempts - 1, 62);
        var initialTicks = _options.InitialDelay.Ticks;
        var maximumTicks = _options.MaximumDelay.Ticks;

        var baseTicks = exponent >= 62 || initialTicks > (maximumTicks >> exponent)
            ? maximumTicks
            : initialTicks << exponent;

        if (_options.DeterministicJitterPercent == 0 || baseTicks >= maximumTicks)
            return TimeSpan.FromTicks(baseTicks);

        var jitterWindowTicks = (baseTicks / 100) * _options.DeterministicJitterPercent;
        var bucket = BitConverter.ToUInt32(eventId.ToByteArray(), 0) % 1001;
        var jitterTicks = (jitterWindowTicks / 1000) * bucket;
        var boundedJitterTicks = Math.Min(maximumTicks - baseTicks, jitterTicks);

        return TimeSpan.FromTicks(baseTicks + boundedJitterTicks);
    }

    public DateTime CalculateNextAvailabilityUtc(
        int completedAttempts,
        Guid eventId,
        DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("nowUtc debe expresarse en UTC.", nameof(nowUtc));

        var delay = CalculateDelay(completedAttempts, eventId);
        if (nowUtc.Ticks > DateTime.MaxValue.Ticks - delay.Ticks)
            throw new InvalidOperationException("La próxima disponibilidad excede el rango de DateTime.");

        return nowUtc.Add(delay);
    }
}
