using InventoryApp.Application.Services;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N14AjusteInventarioServiceCutoverIntegrationTests
{
    [Fact]
    public void Constructor_debe_consumir_el_orquestador_de_existencia_autoritativa()
    {
        var constructor = typeof(AjusteInventarioService)
            .GetConstructors()
            .Single();

        var tipos = constructor
            .GetParameters()
            .Select(p => p.ParameterType)
            .ToArray();

        Assert.Contains(typeof(AjusteInventarioExistenciaCutoverService), tipos);
    }
}
