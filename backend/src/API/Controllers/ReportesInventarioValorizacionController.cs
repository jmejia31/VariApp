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
[Route("inventario/reportes/valorizacion")]
public sealed class ReportesInventarioValorizacionController : ControllerBase
{
    private readonly IFinanzasService _finanzas;
    private readonly IPermisoService _permisos;

    public ReportesInventarioValorizacionController(
        IFinanzasService finanzas,
        IPermisoService permisos)
    {
        _finanzas = finanzas;
        _permisos = permisos;
    }

    [HttpGet("resumen")]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetResumen()
    {
        await _permisos.VerificarPermisoAsync(ModuloSistema.Finanzas, AccionPermiso.Ver);
        var resumen = await _finanzas.GetResumenAsync();

        var resultado = new ReporteInventarioValorizacionResumenDto
        {
            ValorInventarioCosto = resumen.ValorInventarioCosto,
            ValorInventarioCostoMercaderia = resumen.ValorInventarioCostoMercaderia,
            ValorInventarioCostoInsumosAdministrativos = resumen.ValorInventarioCostoInsumosAdministrativos,
            ValorPotencialVentaMercaderia = resumen.ValorPotencialVentaMercaderia
        };

        return Ok(ApiResponse<ReporteInventarioValorizacionResumenDto>.Ok(resultado));
    }
}
