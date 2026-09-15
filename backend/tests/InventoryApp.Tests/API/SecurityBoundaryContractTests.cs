using System.Reflection;
using InventoryApp.API.Controllers;
using InventoryApp.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace InventoryApp.Tests.API;

public sealed class SecurityBoundaryContractTests
{
    [Fact]
    public void PagosOnline_ExigeAutenticacion_Y_NoExponeAccionesAnonimas()
    {
        var type = typeof(PagosOnlineController);

        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(type.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.DoesNotContain(
            type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
            method => method.GetCustomAttribute<AllowAnonymousAttribute>() is not null);
    }

    [Fact]
    public void CargasMasivas_ExigeAutenticacion_Y_LimitaElUpload()
    {
        var type = typeof(CargasMasivasController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(type.GetCustomAttribute<AllowAnonymousAttribute>());

        var validar = type.GetMethod(nameof(CargasMasivasController.Validar));
        Assert.NotNull(validar);

        var requestLimit = validar!.GetCustomAttribute<RequestSizeLimitAttribute>();
        var formLimit = validar.GetCustomAttribute<RequestFormLimitsAttribute>();

        Assert.NotNull(requestLimit);
        Assert.NotNull(formLimit);
        Assert.InRange(requestLimit!.Bytes, 1, 10 * 1024 * 1024);
        Assert.InRange(formLimit!.MultipartBodyLengthLimit, 1, 10 * 1024 * 1024);
    }

    [Fact]
    public void TiendaPublica_PermaneceAnonima_PeroConRateLimitExplicito()
    {
        var type = typeof(TiendaController);

        Assert.NotNull(type.GetCustomAttribute<AllowAnonymousAttribute>());
        var rateLimit = type.GetCustomAttribute<EnableRateLimitingAttribute>();
        Assert.NotNull(rateLimit);
        Assert.Equal("AuthLogin", rateLimit!.PolicyName);
    }

    [Fact]
    public void WebhookPublico_LimitaAnonimatoAlMetodoDeIngreso()
    {
        var type = typeof(InboundWebhooksController);
        Assert.Null(type.GetCustomAttribute<AllowAnonymousAttribute>());

        var receive = type.GetMethod(nameof(InboundWebhooksController.ReceiveAsync));
        Assert.NotNull(receive);
        Assert.NotNull(receive!.GetCustomAttribute<AllowAnonymousAttribute>());

        var anonymousDeclaredMethods = type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            .Select(method => method.Name)
            .ToArray();

        Assert.Equal(new[] { nameof(InboundWebhooksController.ReceiveAsync) }, anonymousDeclaredMethods);
    }
}
