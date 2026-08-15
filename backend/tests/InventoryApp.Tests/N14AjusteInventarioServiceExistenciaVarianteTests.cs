using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N14AjusteInventarioServiceExistenciaVarianteTests
{
    [Fact]
    public void AjusteInventarioService_DebeDependerDelServicioDeConcurrenciaPorExistencia()
    {
        var constructor = typeof(AjusteInventarioService).GetConstructors().Single();
        var tipos = constructor.GetParameters().Select(p => p.ParameterType).ToArray();

        Assert.Contains(typeof(IExistenciaVarianteConcurrencyService), tipos);
        Assert.DoesNotContain(typeof(IInventarioConcurrencyService), tipos);
    }
}
