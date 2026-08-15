using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N14AjusteInventarioServiceCutoverContractTests
{
    [Fact]
    public void Constructor_InyectaServicioDeConcurrenciaDeExistenciaVariante()
    {
        var constructor = Assert.Single(typeof(AjusteInventarioService).GetConstructors());

        Assert.Contains(
            constructor.GetParameters(),
            parametro => parametro.ParameterType == typeof(IExistenciaVarianteConcurrencyService));
    }

    [Fact]
    public void Servicio_ConservaDependenciaFisicaAutoritativa()
    {
        var campo = typeof(AjusteInventarioService)
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .SingleOrDefault(c => c.FieldType == typeof(IExistenciaVarianteConcurrencyService));

        Assert.NotNull(campo);
    }
}
