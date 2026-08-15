using System.Reflection;
using InventoryApp.Application.Services;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N14AjusteInventarioExistenciaCutoverContractTests
{
    [Fact]
    public void Cutover_service_expone_operaciones_fisicas_de_confirmacion_y_reversion()
    {
        var type = typeof(AjusteInventarioExistenciaCutoverService);

        var bloquearConfirmacion = type.GetMethod(
            nameof(AjusteInventarioExistenciaCutoverService.BloquearParaConfirmacionAsync),
            BindingFlags.Instance | BindingFlags.Public);
        var bloquearReversion = type.GetMethod(
            nameof(AjusteInventarioExistenciaCutoverService.BloquearParaReversionAsync),
            BindingFlags.Instance | BindingFlags.Public);
        var aplicarConfirmacion = type.GetMethod(
            nameof(AjusteInventarioExistenciaCutoverService.AplicarObjetivoConfirmacionAsync),
            BindingFlags.Instance | BindingFlags.Public);
        var aplicarReversion = type.GetMethod(
            nameof(AjusteInventarioExistenciaCutoverService.AplicarReversionAsync),
            BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(bloquearConfirmacion);
        Assert.NotNull(bloquearReversion);
        Assert.NotNull(aplicarConfirmacion);
        Assert.NotNull(aplicarReversion);
        Assert.Equal(typeof(Task<int>), aplicarConfirmacion!.ReturnType);
        Assert.Equal(typeof(Task<int>), aplicarReversion!.ReturnType);
    }

    [Fact]
    public void AjusteInventarioService_debe_recibir_cutover_fisico_por_constructor()
    {
        var constructor = typeof(AjusteInventarioService)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .Single();

        Assert.Contains(
            constructor.GetParameters(),
            parameter => parameter.ParameterType == typeof(AjusteInventarioExistenciaCutoverService));
    }
}
