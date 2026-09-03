using System.Reflection;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace InventoryApp.Tests.Services;

public class ConciliacionBancariaObservabilityTests
{
    [Fact]
    public void ServiceDeclaraLoggerTipado()
    {
        var constructors = typeof(ConciliacionBancariaService)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        Assert.Contains(constructors, ctor => ctor.GetParameters().Any(p =>
            p.ParameterType == typeof(ILogger<ConciliacionBancariaService>)));
    }

    [Fact]
    public void AuditoriaExponeRegistroDeEventosDeConciliacion()
    {
        var method = typeof(IAuditService).GetMethod("LogAsync", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(7, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
        Assert.Equal(typeof(string), parameters[2].ParameterType);
        Assert.Equal(typeof(string), parameters[3].ParameterType);
        Assert.Equal(typeof(string), parameters[4].ParameterType);
        Assert.Equal(typeof(string), parameters[5].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[6].ParameterType);
    }
}
