using InventoryApp.Domain.Entities;
using Xunit;

namespace InventoryApp.Tests;

public class N310CreditoClienteDomainTests
{
    private static readonly DateTime AhoraUtc = new(2026, 8, 27, 3, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Crear_ConfiguraPoliticaExplicitaSinMutarCliente()
    {
        var cliente = CrearCliente();

        var credito = CreditoCliente.Crear(cliente, "hnl", 1000m, 30, 80m);

        Assert.Equal(cliente.Id, credito.ClienteId);
        Assert.Same(cliente, credito.Cliente);
        Assert.Equal("HNL", credito.Moneda);
        Assert.Equal(1000m, credito.LimiteCredito);
        Assert.Equal(30, credito.DiasCredito);
        Assert.Equal(80m, credito.PorcentajeAlerta);
        Assert.False(credito.BloqueadoAutomaticamente);
        Assert.True(cliente.Activo);
    }

    [Theory]
    [InlineData(-1, 30, 80)]
    [InlineData(1000, -1, 80)]
    [InlineData(1000, 30, 0)]
    [InlineData(1000, 30, 101)]
    public void Crear_PoliticaInvalida_FallaCerrado(decimal limite, int dias, decimal alerta)
    {
        var cliente = CrearCliente();
        Assert.ThrowsAny<ArgumentException>(() => CreditoCliente.Crear(cliente, "HNL", limite, dias, alerta));
    }

    [Fact]
    public void EvaluarBloqueoAutomatico_BloqueaYDesbloqueaSegunLimite()
    {
        var credito = CrearCredito();

        credito.EvaluarBloqueoAutomatico(1000.01m, AhoraUtc);
        Assert.True(credito.BloqueadoAutomaticamente);
        Assert.Equal("LIMITE_CREDITO_EXCEDIDO", credito.MotivoBloqueo);

        credito.EvaluarBloqueoAutomatico(900m, AhoraUtc.AddMinutes(1));
        Assert.False(credito.BloqueadoAutomaticamente);
        Assert.Null(credito.MotivoBloqueo);
    }

    [Fact]
    public void ObtenerCreditoDisponible_RechazaMonedaDiferente()
    {
        var credito = CrearCredito();
        Assert.Throws<InvalidOperationException>(() =>
            credito.ObtenerCreditoDisponible(100m, "USD", AhoraUtc));
    }

    [Fact]
    public void DebeAlertar_UsaUmbralConfigurableSinThresholdHardcodeado()
    {
        var credito = CrearCredito();

        Assert.False(credito.DebeAlertar(799.99m));
        Assert.True(credito.DebeAlertar(800m));
    }

    [Fact]
    public void AutorizarExcepcion_ExtiendeDisponibleSoloMientrasEstaVigente()
    {
        var credito = CrearCredito();
        credito.EvaluarBloqueoAutomatico(1100m, AhoraUtc);
        Assert.False(credito.PuedeConsumir(1100m, 1m, "HNL", AhoraUtc));

        credito.AutorizarExcepcion(250m, AhoraUtc.AddHours(2), "supervisor", AhoraUtc);

        Assert.True(credito.PuedeConsumir(1100m, 250m, "HNL", AhoraUtc.AddMinutes(1)));
        Assert.False(credito.PuedeConsumir(1100m, 250m, "HNL", AhoraUtc.AddHours(3)));
        Assert.Equal("supervisor", credito.ExcepcionAutorizadaPor);
    }

    [Fact]
    public void AutorizarExcepcion_ExigeActorYVigenciaUtcFutura()
    {
        var credito = CrearCredito();

        Assert.Throws<ArgumentException>(() =>
            credito.AutorizarExcepcion(100m, AhoraUtc.AddHours(1), " ", AhoraUtc));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            credito.AutorizarExcepcion(100m, AhoraUtc, "supervisor", AhoraUtc));
        Assert.Throws<ArgumentException>(() =>
            credito.AutorizarExcepcion(100m, DateTime.Now.AddHours(1), "supervisor", AhoraUtc));
    }

    private static CreditoCliente CrearCredito() =>
        CreditoCliente.Crear(CrearCliente(), "HNL", 1000m, 30, 80m);

    private static Cliente CrearCliente() => new()
    {
        Id = 44,
        Nombre = "Cliente Crédito",
        Activo = true,
        TipoClienteId = 1
    };
}
