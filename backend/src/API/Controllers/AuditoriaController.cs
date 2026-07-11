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
[RequierePermiso(ModuloSistema.Auditoria, AccionPermiso.Ver)]
[Route("auditoria")]
public class AuditoriaController : ControllerBase
{
    private readonly IAuditoriaService _service;

    public AuditoriaController(IAuditoriaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetFiltered([FromQuery] AuditoriaFiltroDto filtro)
    {
        var resultado = await _service.GetFilteredAsync(filtro);
        return Ok(ApiResponse<PagedResult<RegistroAuditoriaDto>>.Ok(resultado));
    }
}
