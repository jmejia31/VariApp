using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/outbox/tenants/{empresaId:int}")]
public sealed class MensajesOutboxController : ControllerBase
{
    private readonly IMensajeOutboxService _service;
    private readonly AppDbContext _db;

    public MensajesOutboxController(IMensajeOutboxService service, AppDbContext db)
    {
        _service = service;
        _db = db;
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Crear)]
    public async Task<IActionResult> Registrar(
        int empresaId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] RegistrarMensajeOutboxRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Problema(
                StatusCodes.Status400BadRequest,
                "Idempotency-Key requerida",
                "Idempotency-Key es obligatoria para registrar un mensaje outbox.",
                "OUTBOX_IDEMPOTENCY_KEY_REQUIRED");
        }

        try
        {
            var mensaje = await _service.RegistrarAsync(
                empresaId,
                request,
                idempotencyKey.Trim(),
                cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return Accepted(ApiResponse<MensajeOutboxRegistroResponse>.Ok(
                new MensajeOutboxRegistroResponse(
                    mensaje.EventoId,
                    mensaje.EmpresaId,
                    mensaje.Estado.ToString())));
        }
        catch (ConflictException)
        {
            return Problema(
                StatusCodes.Status409Conflict,
                "Conflicto de idempotencia",
                "La clave de idempotencia ya fue utilizada para esta empresa.",
                "OUTBOX_IDEMPOTENCY_CONFLICT");
        }
        catch (ForbiddenAccessException)
        {
            return Problema(
                StatusCodes.Status403Forbidden,
                "Acceso denegado",
                "No existe autoridad tenant activa para la empresa solicitada.",
                "OUTBOX_TENANT_FORBIDDEN");
        }
        catch (ArgumentException)
        {
            return Problema(
                StatusCodes.Status400BadRequest,
                "Solicitud inválida",
                "Los datos del mensaje outbox no cumplen el contrato requerido.",
                "OUTBOX_INVALID_REQUEST");
        }
        catch (DbUpdateException)
        {
            // La restricción única (EmpresaId, ClaveIdempotencia) es la última
            // defensa ante carreras interleaved. No se filtran mensajes del provider.
            return Problema(
                StatusCodes.Status409Conflict,
                "Conflicto de idempotencia",
                "No fue posible registrar el mensaje porque la clave ya fue consumida.",
                "OUTBOX_IDEMPOTENCY_RACE");
        }
    }

    private ObjectResult Problema(int status, string title, string detail, string code)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{status}",
            Instance = Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;

        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }
}

public sealed record MensajeOutboxRegistroResponse(Guid EventoId, int EmpresaId, string Estado);
