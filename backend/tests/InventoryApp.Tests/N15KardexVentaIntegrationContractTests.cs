using InventoryApp.Application.Common;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N15KardexVentaIntegrationContractTests
{
    [Fact]
    public void Registrar_de_venta_debe_consumir_writer_canonico_de_Kardex()
    {
        var constructor = typeof(VentaKardexMovimientoRegistrar)
            .GetConstructors()
            .Single();

        Assert.Contains(
            constructor.GetParameters(),
            parametro => parametro.ParameterType == typeof(IKardexMovimientoWriter));
    }

    [Theory]
    [InlineData(31, "venta:31:confirmar")]
    [InlineData(31, "venta:31:anular")]
    public void Correlacion_venta_permanece_deterministica_por_operacion(int ventaId, string esperado)
    {
        var actual = esperado.EndsWith(":confirmar", StringComparison.Ordinal)
            ? KardexCorrelationId.VentaConfirmar(ventaId)
            : KardexCorrelationId.VentaAnular(ventaId);

        Assert.Equal(esperado, actual);
    }
}
