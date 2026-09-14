using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Api.Controllers;

[ApiController]
[Route("api/webhooks/{empresaId:int}/{proveedor}")]
public sealed class InboundWebhooksController : ControllerBase
{
    private const string SignatureHeader = "X-Webhook-Signature";
    private const string CorrelationHeader = "X-Correlation-Id";

    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public InboundWebhooksController(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveAsync(
        int empresaId,
        string proveedor,
        CancellationToken cancellationToken)
    {
        if (empresaId <= 0 || !InboundWebhookIngressService.TryNormalizeProvider(proveedor, out var normalizedProvider))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud de webhook inválida.",
                detail: "La identidad del tenant o proveedor no es válida.");
        }

        var secret = _configuration[$"Webhooks:{empresaId}:{normalizedProvider}:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Webhook no configurado.",
                detail: "No existe una configuración activa para este origen.");
        }

        Request.EnableBuffering();
        using var reader = new StreamReader(
            Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        var signature = Request.Headers[SignatureHeader].ToString();
        var correlationId = Request.Headers[CorrelationHeader].ToString();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = HttpContext.TraceIdentifier;

        var service = new InboundWebhookIngressService(_db, secret);
        var result = await service.ReceiveAsync(
            empresaId,
            normalizedProvider,
            rawBody,
            signature,
            correlationId,
            cancellationToken);

        return result.Kind switch
        {
            InboundWebhookIngressKind.Accepted => Accepted(new
            {
                result.WebhookId,
                Estado = "Recibido",
                Idempotente = false
            }),
            InboundWebhookIngressKind.Duplicate => Ok(new
            {
                result.WebhookId,
                Estado = "Recibido",
                Idempotente = true
            }),
            InboundWebhookIngressKind.InvalidSignature => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Firma de webhook inválida.",
                detail: "La solicitud no superó la validación criptográfica."),
            InboundWebhookIngressKind.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Webhook duplicado en conflicto.",
                detail: "El identificador externo ya existe con una huella de payload distinta."),
            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud de webhook inválida.",
                detail: "El cuerpo del webhook no cumple el contrato de entrada.")
        };
    }
}

public sealed class InboundWebhookIngressService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly byte[] _secret;

    public InboundWebhookIngressService(AppDbContext db, string secret)
    {
        _db = db;
        _secret = Encoding.UTF8.GetBytes(secret ?? throw new ArgumentNullException(nameof(secret)));
    }

    public async Task<InboundWebhookIngressResult> ReceiveAsync(
        int empresaId,
        string normalizedProvider,
        string rawBody,
        string? suppliedSignature,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        if (!IsValidSignature(rawBody, suppliedSignature))
            return InboundWebhookIngressResult.InvalidSignature();

        InboundWebhookEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<InboundWebhookEnvelope>(rawBody, JsonOptions);
        }
        catch (JsonException)
        {
            return InboundWebhookIngressResult.InvalidRequest();
        }

        if (envelope is null ||
            string.IsNullOrWhiteSpace(envelope.EventoExternoId) ||
            string.IsNullOrWhiteSpace(envelope.TipoEvento) ||
            envelope.EmitidoEnUtc.Kind != DateTimeKind.Utc)
        {
            return InboundWebhookIngressResult.InvalidRequest();
        }

        var empresaExiste = await _db.Set<Empresa>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == empresaId, cancellationToken);
        if (!empresaExiste)
            return InboundWebhookIngressResult.InvalidRequest();

        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody)));

        var existente = await _db.Set<WebhookEntrante>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.EmpresaId == empresaId &&
                     x.Proveedor == normalizedProvider &&
                     x.EventoExternoId == envelope.EventoExternoId.Trim(),
                cancellationToken);

        if (existente is not null)
        {
            return existente.CoincidePayload(payloadHash)
                ? InboundWebhookIngressResult.Duplicate(existente.Id)
                : InboundWebhookIngressResult.Conflict(existente.Id);
        }

        WebhookEntrante entity;
        try
        {
            entity = WebhookEntrante.CrearVerificado(
                empresaId,
                normalizedProvider,
                envelope.EventoExternoId,
                envelope.TipoEvento,
                payloadHash,
                correlationId,
                envelope.EmitidoEnUtc,
                DateTime.UtcNow);
        }
        catch (ArgumentException)
        {
            return InboundWebhookIngressResult.InvalidRequest();
        }

        _db.Set<WebhookEntrante>().Add(entity);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return InboundWebhookIngressResult.Accepted(entity.Id);
        }
        catch (DbUpdateException)
        {
            _db.Entry(entity).State = EntityState.Detached;
            var concurrente = await _db.Set<WebhookEntrante>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.EmpresaId == empresaId &&
                         x.Proveedor == normalizedProvider &&
                         x.EventoExternoId == envelope.EventoExternoId.Trim(),
                    cancellationToken);

            if (concurrente is null)
                throw;

            return concurrente.CoincidePayload(payloadHash)
                ? InboundWebhookIngressResult.Duplicate(concurrente.Id)
                : InboundWebhookIngressResult.Conflict(concurrente.Id);
        }
    }

    internal static bool TryNormalizeProvider(string? provider, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(provider))
            return false;

        var candidate = provider.Trim().ToLowerInvariant();
        if (candidate.Length > WebhookEntrante.LongitudMaximaProveedor ||
            candidate.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.')))
        {
            return false;
        }

        normalized = candidate;
        return true;
    }

    private bool IsValidSignature(string rawBody, string? suppliedSignature)
    {
        if (string.IsNullOrWhiteSpace(rawBody) || string.IsNullOrWhiteSpace(suppliedSignature))
            return false;

        var signature = suppliedSignature.Trim();
        if (signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            signature = signature[7..];

        if (signature.Length != 64)
            return false;

        Span<byte> suppliedBytes = stackalloc byte[32];
        if (!Convert.TryFromHexString(signature, suppliedBytes, out var bytesWritten) || bytesWritten != 32)
            return false;

        using var hmac = new HMACSHA256(_secret);
        var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        return CryptographicOperations.FixedTimeEquals(expected, suppliedBytes);
    }
}

public sealed class InboundWebhookEnvelope
{
    public string EventoExternoId { get; init; } = string.Empty;
    public string TipoEvento { get; init; } = string.Empty;
    public DateTime EmitidoEnUtc { get; init; }
}

public enum InboundWebhookIngressKind
{
    Accepted,
    Duplicate,
    InvalidSignature,
    Conflict,
    InvalidRequest
}

public sealed record InboundWebhookIngressResult(
    InboundWebhookIngressKind Kind,
    int? WebhookId)
{
    public static InboundWebhookIngressResult Accepted(int id) => new(InboundWebhookIngressKind.Accepted, id);
    public static InboundWebhookIngressResult Duplicate(int id) => new(InboundWebhookIngressKind.Duplicate, id);
    public static InboundWebhookIngressResult InvalidSignature() => new(InboundWebhookIngressKind.InvalidSignature, null);
    public static InboundWebhookIngressResult Conflict(int id) => new(InboundWebhookIngressKind.Conflict, id);
    public static InboundWebhookIngressResult InvalidRequest() => new(InboundWebhookIngressKind.InvalidRequest, null);
}
