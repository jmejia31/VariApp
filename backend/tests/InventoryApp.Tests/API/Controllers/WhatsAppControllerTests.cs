using System.Reflection;
using InventoryApp.API.Controllers;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InventoryApp.Tests.API.Controllers;

public sealed class WhatsAppControllerTests
{
    private const string VerifyToken = "wa-test-verify-token";

    [Fact]
    public async Task Webhook_rejects_missing_or_incorrect_token_and_accepts_exact_token()
    {
        await using var db = CreateDb();
        var controller = CreateController(db, ConfigurationWithToken(), ScopeFor(null));

        controller.Request.Headers.Remove("X-WhatsApp-Verify-Token");
        var missing = Assert.IsType<ObjectResult>(controller.Webhook());
        Assert.Equal(StatusCodes.Status401Unauthorized, missing.StatusCode);

        controller.Request.Headers["X-WhatsApp-Verify-Token"] = "wrong-token";
        var incorrect = Assert.IsType<ObjectResult>(controller.Webhook());
        Assert.Equal(StatusCodes.Status401Unauthorized, incorrect.StatusCode);

        controller.Request.Headers["X-WhatsApp-Verify-Token"] = VerifyToken;
        var accepted = Assert.IsType<OkObjectResult>(controller.Webhook());
        var payload = Assert.IsType<WhatsAppWebhookResponse>(accepted.Value);
        Assert.Equal("accepted", payload.Status);
    }

    [Fact]
    public async Task Webhook_fails_closed_when_verify_token_is_not_configured()
    {
        await using var db = CreateDb();
        var controller = CreateController(
            db,
            new ConfigurationBuilder().Build(),
            ScopeFor(null));

        controller.Request.Headers["X-WhatsApp-Verify-Token"] = VerifyToken;
        var unavailable = Assert.IsType<ObjectResult>(controller.Webhook());
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
    }

    [Fact]
    public void Iniciar_whatsapp_requires_authorization()
    {
        var method = typeof(WhatsAppController).GetMethod(nameof(WhatsAppController.IniciarAsync));
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public async Task Iniciar_whatsapp_rejects_unverified_tenant_scope()
    {
        await using var db = CreateDb();
        var controller = CreateController(db, ConfigurationWithToken(), ScopeFor(null));

        var action = await controller.IniciarAsync(new IniciarWhatsAppRequest(1), CancellationToken.None);

        Assert.IsType<ForbidResult>(action);
    }

    [Fact]
    public async Task Iniciar_whatsapp_returns_not_found_without_active_tenant_configuration()
    {
        await using var db = CreateDb();
        var controller = CreateController(
            db,
            ConfigurationWithToken(),
            ScopeFor(new UsuarioTenantScopeActual(7, 1, 1, "Admin", true)));

        var action = await controller.IniciarAsync(new IniciarWhatsAppRequest(1), CancellationToken.None);

        var missing = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status404NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Iniciar_whatsapp_returns_explicit_status_without_fabricating_qr()
    {
        await using var db = CreateDb();
        var empresa = new Empresa("Tenant WhatsApp");
        db.Set<Empresa>().Add(empresa);
        await db.SaveChangesAsync();
        db.Set<ConfiguracionWhatsAppEmpresa>().Add(new ConfiguracionWhatsAppEmpresa(
            empresa.Id,
            "+50499999999",
            "vault://whatsapp/token",
            "vault://whatsapp/webhook"));
        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            ConfigurationWithToken(),
            ScopeFor(new UsuarioTenantScopeActual(7, empresa.Id, 1, "Admin", true)));

        var action = await controller.IniciarAsync(
            new IniciarWhatsAppRequest(empresa.Id),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action);
        var payload = Assert.IsType<IniciarWhatsAppResponse>(ok.Value);
        Assert.Equal("CONFIGURADA_SIN_SESION", payload.Status);
        Assert.Null(payload.Qr);
        Assert.Equal("+50499999999", payload.NumeroTelefonoE164);
        Assert.True(payload.RequiereProveedorSesion);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static IConfiguration ConfigurationWithToken() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WA_WEBHOOK_VERIFY_TOKEN"] = VerifyToken
            })
            .Build();

    private static IUsuarioScopeService ScopeFor(UsuarioTenantScopeActual? result)
    {
        var mock = new Mock<IUsuarioScopeService>();
        mock.Setup(x => x.ObtenerActualAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return mock.Object;
    }

    private static WhatsAppController CreateController(
        AppDbContext db,
        IConfiguration configuration,
        IUsuarioScopeService scope)
    {
        var controller = new WhatsAppController(
            db,
            configuration,
            scope,
            NullLogger<WhatsAppController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    TraceIdentifier = "n76d-test-correlation"
                }
            }
        };
        return controller;
    }
}
