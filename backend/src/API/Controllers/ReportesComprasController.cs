using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("compras/reportes")]
public sealed class ReportesComprasController : ControllerBase
{
    private readonly ReporteComprasService _service;

    public ReportesComprasController(AppDbContext context) => _service = new ReporteComprasService(context);

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
