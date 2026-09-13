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
    private readonly IUsuarioScopeService _usuarioScope;
    private readonly IAuditoriaService _auditoria;

    public ReportesInventarioStockHealthController(
        AppDbContext context,
        IUsuarioScopeService usuarioScope,
        IAuditoriaService auditoria)
    {
        _reportes = new ReporteInventarioService(context, usuarioScope);
        _usuarioScope = usuarioScope;
        _auditoria = auditoria;
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

        if (!await ReporteInventarioScopeGuard.CanUseExplicitPhysicalScopeAsync(filtro, _usuarioScope))
            return Forbid();

        var resultado = await _reportes.ObtenerStockHealthAsync(filtro, cancellationToken);

        await _auditoria.RegistrarAsync(
            ModuloSistema.Inventario,
            AccionPermiso.Ver,
            "Consulta autorizada de stock-health de inventario.",
            entidad: "ReporteInventarioStockHealth",
            valoresNuevos: new
            {
                filtro.AlmacenId,
                filtro.UbicacionAlmacenId,
                filtro.SucursalId,
                filtro.Desde,
                filtro.Hasta,
                filtro.Dias,
                filtro.Page,
                filtro.PageSize,
                CorrelationId = HttpContext.TraceIdentifier
            });

        return Ok(ApiResponse<PagedResult<ReporteInventarioStockHealthDto>>.Ok(resultado));
    }
}
