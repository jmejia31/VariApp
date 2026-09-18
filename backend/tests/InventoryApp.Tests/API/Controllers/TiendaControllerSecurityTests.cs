using System.Reflection;
using InventoryApp.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace InventoryApp.Tests.API.Controllers;

public sealed class TiendaControllerSecurityTests
{
    [Fact]
    public void TiendaPublica_ConservaAnonimatoPeroAplicaRateLimitPorIp()
    {
        var type = typeof(TiendaController);

        Assert.NotNull(type.GetCustomAttribute<AllowAnonymousAttribute>());

        var rateLimit = type.GetCustomAttribute<EnableRateLimitingAttribute>();
        Assert.NotNull(rateLimit);
        Assert.Equal("AuthLogin", rateLimit!.PolicyName);
    }
}
