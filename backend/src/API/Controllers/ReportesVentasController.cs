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
[Route("ventas/reportes")]
public sealed class ReportesVentasController : ControllerBase
{
    private readonly IReporteVentasService _service;

    public ReportesVentasController(IReporteVentasService service)
    {
        _service = service;
    }

    [HttpGet("resumen")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetResumen(
        [FromQuery] ReporteVentasFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        var errores = ReporteVentasQueryRules.Validate(filtro);
        if (errores.Count > 0)
            return BadRequest(ApiResponse<object>.Fail(string.Join(" ", errores)));

        var resultado = await _service.ObtenerResumenAsync(filtro, cancellationToken);
        return Ok(ApiResponse<ReporteVentasResumenDto>.Ok(resultado));
    }

    [HttpGet("detalle")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetDetalle(
        [FromQuery] ReporteVentasFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        var errores = ReporteVentasQueryRules.Validate(filtro);
        if (errores.Count > 0)
            return BadRequest(ApiResponse<object>.Fail(string.Join(" ", errores)));

        var resultado = await _service.ObtenerDetallePaginadoAsync(filtro, cancellationToken);
        return Ok(ApiResponse<PagedResult<ReporteVentasDetalleDto>>.Ok(resultado));
    }
}
