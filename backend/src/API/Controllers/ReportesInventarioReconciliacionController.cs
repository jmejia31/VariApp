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
[Route("inventario/reportes/reconciliacion")]
public sealed class ReportesInventarioReconciliacionController : ControllerBase
{
    private readonly ReporteInventarioService _reportes;
    private readonly IPermisoService _permisos;

    public ReportesInventarioReconciliacionController(
        AppDbContext context,
        IUsuarioScopeService usuarioScope,
        IPermisoService permisos)
    {
        _reportes = new ReporteInventarioService(context, usuarioScope);
        _permisos = permisos;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Ver)]
    public async Task<IActionResult> Get(
        [FromQuery] ReporteInventarioReconciliacionFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var error = ReporteInventarioQueryRules.Validate(filtro, "Fecha");
        if (error is not null)
            return BadRequest(ApiResponse<object>.Fail("Consulta de reconciliación inválida.", new() { error }));

        var resultado = await _reportes.ObtenerReporteReconciliacionAsync(filtro, cancellationToken);
        if (!await _permisos.TienePermisoAsync(ModuloSistema.Finanzas, AccionPermiso.Ver))
        {
            foreach (var row in resultado.Items)
            {
                row.CostoUnitario = null;
                row.ImpactoCosto = null;
            }
        }

        return Ok(ApiResponse<PagedResult<ReporteInventarioReconciliacionDto>>.Ok(resultado));
    }
}
