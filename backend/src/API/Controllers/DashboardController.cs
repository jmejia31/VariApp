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
[Route("dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("resumen")]
    [RequierePermiso(ModuloSistema.Dashboard, AccionPermiso.Ver)]
    public async Task<IActionResult> GetResumen()
    {
        var resumen = await _dashboardService.GetResumenAsync();
        return Ok(ApiResponse<DashboardResumenDto>.Ok(resumen));
    }
}
