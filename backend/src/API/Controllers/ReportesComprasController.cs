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
[Route("compras/reportes")]
public sealed class ReportesComprasController : ControllerBase
{
    private readonly IReporteComprasService _service;

    public ReportesComprasController(IReporteComprasService service) => _service = service;

    [HttpGet("detalle")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> GetDetalle(
        [FromQuery] ReporteComprasFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var resultado = await _service.ObtenerDetallePaginadoAsync(filtro, cancellationToken);
            return Ok(ApiResponse<PagedResult<ReporteComprasDetalleDto>>.Ok(resultado));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
