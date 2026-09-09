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
[Route("inventario/reportes/stock-health")]
public sealed class ReportesInventarioStockHealthController : ControllerBase
{
    private readonly IReporteInventarioService _reportes;

    public ReportesInventarioStockHealthController(IReporteInventarioService reportes)
    {
        _reportes = reportes;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Ver)]
    public async Task<IActionResult> Get(
        [FromQuery] ReporteInventarioStockHealthFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var error = ReporteInventarioQueryRules.ValidateStockHealth(filtro);
        if (error is not null)
            return BadRequest(ApiResponse<object>.Fail("Consulta de stock-health inválida.", new() { error }));

        var resultado = await _reportes.ObtenerStockHealthAsync(filtro, cancellationToken);
        return Ok(ApiResponse<PagedResult<ReporteInventarioStockHealthDto>>.Ok(resultado));
    }
}
