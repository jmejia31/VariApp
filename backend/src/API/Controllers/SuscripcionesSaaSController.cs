using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

/// <summary>
/// Superficie API mínima N6.9.D. empresaId expresa selección de tenant, no
/// autoridad; ISuscripcionesSaaSService debe verificar membresía server-side antes
/// de consultar o mutar persistencia.
/// </summary>
[ApiController]
[Authorize]
[Route("api/saas/tenants/{empresaId:int}")]
public sealed class SuscripcionesSaaSController : ControllerBase
{
    private readonly ISuscripcionesSaaSService _service;

    public SuscripcionesSaaSController(ISuscripcionesSaaSService service)
    {
        _service = service;
    }

    [HttpPost("onboarding")]
    public async Task<IActionResult> Onboarding(
        int empresaId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] OnboardingSuscripcionSaaSRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(ApiResponse<string>.Fail(
                SuscripcionSaaSErrorCodes.IdempotencyKeyRequerida,
                "Idempotency-Key es obligatorio para onboarding."));
        }

        var suscripcion = await _service.OnboardingAsync(
            empresaId,
            request,
            idempotencyKey.Trim(),
            cancellationToken);

        return Ok(ApiResponse<SuscripcionSaaSDto>.Ok(suscripcion));
    }

    [HttpGet("suscripcion")]
    public async Task<IActionResult> ObtenerActual(
        int empresaId,
        [FromQuery] DateTime? instanteUtc,
        CancellationToken cancellationToken)
    {
        var suscripcion = await _service.ObtenerActualAsync(
            empresaId,
            instanteUtc,
            cancellationToken);

        return Ok(ApiResponse<SuscripcionSaaSDto>.Ok(suscripcion));
    }

    [HttpGet("limites")]
    public async Task<IActionResult> ObtenerLimites(
        int empresaId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 50,
        [FromQuery] string? clave = null,
        CancellationToken cancellationToken = default)
    {
        var query = new LimitesSuscripcionSaaSQuery(pagina, tamanoPagina, clave).Normalizada();
        var limites = await _service.ObtenerLimitesAsync(empresaId, query, cancellationToken);
        return Ok(ApiResponse<PaginaSuscripcionSaaSDto<LimiteSuscripcionSaaSDto>>.Ok(limites));
    }
}
