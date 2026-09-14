using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InventoryApp.Api.Controllers;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace InventoryApp.Tests.API;

public sealed class InboundWebhookIngressTests
{
    private const string Secret = "n75d-test-secret";

    [Fact]
    public async Task Invalid_signature_is_rejected_before_persistence()
    {
        await using var db = await CreateDbAsync();
        var service = new InboundWebhookIngressService(db, Secret);
        var body = Body("evt-invalid", "inventory.updated");

        var result = await service.ReceiveAsync(
            1,
            "provider-a",
            body,
            "sha256=" + new string('0', 64),
            "corr-invalid",
            CancellationToken.None);

        Assert.Equal(InboundWebhookIngressKind.InvalidSignature, result.Kind);
        Assert.Empty(await db.Set<WebhookEntrante>().ToListAsync());
    }

    [Fact]
    public async Task Valid_signature_persists_received_webhook_without_secret_or_raw_payload()
    {
        await using var db = await CreateDbAsync();
        var service = new InboundWebhookIngressService(db, Secret);
        var body = Body("evt-accepted", "inventory.updated");

        var result = await service.ReceiveAsync(
            1,
            "provider-a",
            body,
            Signature(body),
            "corr-accepted",
            CancellationToken.None);

        Assert.Equal(InboundWebhookIngressKind.Accepted, result.Kind);
        var stored = Assert.Single(await db.Set<WebhookEntrante>().ToListAsync());
        Assert.Equal(1, stored.EmpresaId);
        Assert.Equal("provider-a", stored.Proveedor);
        Assert.Equal("evt-accepted", stored.EventoExternoId);
        Assert.Equal(EstadoWebhookEntrante.Recibido, stored.Estado);
        Assert.Equal("corr-accepted", stored.CorrelationId);
        Assert.Equal(64, stored.PayloadHash.Length);
        Assert.DoesNotContain(Secret, stored.PayloadHash, StringComparison.Ordinal);
        Assert.DoesNotContain("inventory.updated", stored.PayloadHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Same_tenant_provider_event_and_payload_is_idempotent()
    {
        await using var db = await CreateDbAsync();
        var service = new InboundWebhookIngressService(db, Secret);
        var body = Body("evt-repeat", "inventory.updated");
        var signature = Signature(body);

        var first = await service.ReceiveAsync(
            1, "provider-a", body, signature, "corr-1", CancellationToken.None);
        var second = await service.ReceiveAsync(
            1, "provider-a", body, signature, "corr-2", CancellationToken.None);

        Assert.Equal(InboundWebhookIngressKind.Accepted, first.Kind);
        Assert.Equal(InboundWebhookIngressKind.Duplicate, second.Kind);
        Assert.Equal(first.WebhookId, second.WebhookId);
        Assert.Single(await db.Set<WebhookEntrante>().ToListAsync());
    }

    [Fact]
    public async Task Same_tenant_provider_event_with_different_payload_is_conflict()
    {
        await using var db = await CreateDbAsync();
        var service = new InboundWebhookIngressService(db, Secret);
        var firstBody = Body("evt-conflict", "inventory.updated");
        var secondBody = Body("evt-conflict", "inventory.deleted");

        var first = await service.ReceiveAsync(
            1, "provider-a", firstBody, Signature(firstBody), "corr-1", CancellationToken.None);
        var second = await service.ReceiveAsync(
            1, "provider-a", secondBody, Signature(secondBody), "corr-2", CancellationToken.None);

        Assert.Equal(InboundWebhookIngressKind.Accepted, first.Kind);
        Assert.Equal(InboundWebhookIngressKind.Conflict, second.Kind);
        Assert.Single(await db.Set<WebhookEntrante>().ToListAsync());
    }

    [Fact]
    public async Task Controller_uses_trace_identifier_and_logs_without_sensitive_material()
    {
        await using var db = await CreateDbAsync();
        var body = Body("evt-observable", "inventory.updated");
        var signature = Signature(body);
        var logger = new CaptureLogger<InboundWebhooksController>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Webhooks:1:provider-a:Secret"] = Secret
            })
            .Build();

        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "corr-safe-123"
        };
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        httpContext.Request.Headers["X-Webhook-Signature"] = signature;
        httpContext.Request.Headers["X-Correlation-Id"] = new string('!', 200);

        var controller = new InboundWebhooksController(db, configuration, logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var action = await controller.ReceiveAsync(1, "provider-a", CancellationToken.None);

        Assert.IsType<AcceptedResult>(action);
        var stored = Assert.Single(await db.Set<WebhookEntrante>().ToListAsync());
        Assert.Equal("corr-safe-123", stored.CorrelationId);

        var log = Assert.Single(logger.Messages);
        Assert.Contains("Accepted", log, StringComparison.Ordinal);
        Assert.Contains("corr-safe-123", log, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, log, StringComparison.Ordinal);
        Assert.DoesNotContain(signature, log, StringComparison.Ordinal);
        Assert.DoesNotContain(body, log, StringComparison.Ordinal);
    }

    private static async Task<AppDbContext> CreateDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new AppDbContext(options);
        db.Set<Empresa>().Add(new Empresa("Tenant N7.5.D"));
        await db.SaveChangesAsync();
        return db;
    }

    private static string Body(string eventId, string eventType) =>
        JsonSerializer.Serialize(new
        {
            eventoExternoId = eventId,
            tipoEvento = eventType,
            emitidoEnUtc = new DateTime(2026, 9, 14, 1, 30, 0, DateTimeKind.Utc),
            data = new { sku = "ABC-1", quantity = 2 }
        });

    private static string Signature(string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        return "sha256=" + Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)));
    }

    private sealed class CaptureLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NoopScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NoopScope : IDisposable
        {
            public static NoopScope Instance { get; } = new();
            public void Dispose() { }
        }
    }
}
