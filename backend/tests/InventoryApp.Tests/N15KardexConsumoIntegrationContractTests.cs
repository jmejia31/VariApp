using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N15KardexConsumoIntegrationContractTests
{
    [Fact]
    public void ConsumoInsumoService_debe_consumir_writer_canonico_de_Kardex()
    {
        var constructor = typeof(ConsumoInsumoService)
            .GetConstructors()
            .Single();

        Assert.Contains(
            constructor.GetParameters(),
            parametro => parametro.ParameterType == typeof(IKardexMovimientoWriter));
    }
}
