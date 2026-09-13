using InventoryApp.Application.Services;
using Xunit;

namespace InventoryApp.Tests.Application;

public class MensajeOutboxRetryPolicyTests
{
    private static readonly Guid Evento = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly DateTime Ahora = new(2026, 9, 13, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ConfiguracionInvalida_FallaCerrada()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MensajeOutboxRetryPolicy(new MensajeOutboxRetryOptions(
                0,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMinutes(1))));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MensajeOutboxRetryPolicy(new MensajeOutboxRetryOptions(
                3,
                TimeSpan.FromMinutes(2),
                TimeSpan.FromMinutes(1))));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MensajeOutboxRetryPolicy(new MensajeOutboxRetryOptions(
                3,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMinutes(1),
                26)));
    }

    [Fact]
    public void BackoffExponencial_EsDeterministaYAcotado()
    {
        var policy = CrearPolicy(jitter: 10);

        var primero = policy.CalculateDelay(1, Evento);
        var repetido = policy.CalculateDelay(1, Evento);
        var segundo = policy.CalculateDelay(2, Evento);
        var tardio = policy.CalculateDelay(9, Evento);

        Assert.Equal(primero, repetido);
        Assert.InRange(primero, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(11));
        Assert.True(segundo >= TimeSpan.FromSeconds(20));
        Assert.True(segundo <= TimeSpan.FromSeconds(22));
        Assert.Equal(TimeSpan.FromMinutes(1), tardio);
    }

    [Fact]
    public void LimiteDeIntentos_BloqueaOtroRetry()
    {
        var policy = CrearPolicy(maxAttempts: 3);

        Assert.True(policy.CanRetry(2));
        Assert.False(policy.CanRetry(3));
        Assert.Throws<InvalidOperationException>(() => policy.CalculateDelay(3, Evento));
    }

    [Fact]
    public void ProximaDisponibilidad_UsaUtcYNoPermiteOverflow()
    {
        var policy = CrearPolicy();

        Assert.Equal(Ahora.AddSeconds(10), policy.CalculateNextAvailabilityUtc(1, Evento, Ahora));
        Assert.Throws<ArgumentException>(() =>
            policy.CalculateNextAvailabilityUtc(
                1,
                Evento,
                DateTime.SpecifyKind(Ahora, DateTimeKind.Local)));
        Assert.Throws<InvalidOperationException>(() =>
            policy.CalculateNextAvailabilityUtc(
                1,
                Evento,
                DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)));
    }

    [Fact]
    public void IdentidadDeEventoEsObligatoriaParaJitterReproducible()
    {
        var policy = CrearPolicy();

        Assert.Throws<ArgumentException>(() => policy.CalculateDelay(1, Guid.Empty));
    }

    private static MensajeOutboxRetryPolicy CrearPolicy(int maxAttempts = 3, int jitter = 0) =>
        new(new MensajeOutboxRetryOptions(
            maxAttempts,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMinutes(1),
            jitter));
}
