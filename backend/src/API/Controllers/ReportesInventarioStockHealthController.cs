using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("inventario/reportes/stock-health")]
public sealed class ReportesInventarioStockHealthController : ControllerBase
{
    private readonly ReporteInventarioService _reportes;

    public ReportesInventarioStockHealthController(
        AppDbContext context,
        IUsuarioScopeService usuarioScope)
    {
        _reportes = new ReporteInventarioService(context, usuarioScope);
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
