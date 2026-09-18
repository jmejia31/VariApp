using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs.Contabilidad;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("contabilizacion")]
public sealed class ContabilizacionController : ControllerBase
{
    private readonly IContabilizacionService _service;

    public ContabilizacionController(AppDbContext db, IAuditoriaService auditoria)
    {
        var writer = new AsientoContableWriter(db, auditoria);
        _service = new ContabilizacionService(db, writer);
    }

    [HttpPost("eventos")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> Contabilizar(
        [FromBody] EventoContableDto evento,
        CancellationToken cancellationToken)
    {
        var result = await _service.ContabilizarAsync(evento, cancellationToken);

        return result.Created
            ? StatusCode(StatusCodes.Status201Created, ApiResponse<AsientoContableDto>.Ok(result.Asiento))
            : Ok(ApiResponse<AsientoContableDto>.Ok(result.Asiento));
    }
}
