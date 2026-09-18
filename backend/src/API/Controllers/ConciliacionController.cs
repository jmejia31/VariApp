using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("conciliacion")]
public sealed class ConciliacionController : ControllerBase
{
    private readonly IThreeWayMatchService _service;

    public ConciliacionController(IThreeWayMatchService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet("ordenes-compra/{ordenCompraId:int}/three-way-match")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> EvaluarThreeWayMatch(
        [FromRoute] int ordenCompraId,
        CancellationToken cancellationToken)
    {
        var resultado = await _service.EvaluarAsync(ordenCompraId, cancellationToken);
        return Ok(ApiResponse<ThreeWayMatchResultDto>.Ok(resultado));
    }
}
