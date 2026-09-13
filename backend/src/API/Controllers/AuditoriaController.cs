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
    private readonly ICurrentUserService _currentUser;

    public AuditoriaController(IAuditoriaService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetFiltered([FromQuery] AuditoriaFiltroDto filtro)
    {
        // La auditoría contiene información transversal y sensible de toda la empresa.
        // Aun cuando una matriz de permisos se configure incorrectamente, nunca se
        // entrega a un rol no administrativo.
        if (!_currentUser.EsAdministrador)
            return Forbid();

        var resultado = await _service.GetFilteredAsync(filtro);
        return Ok(ApiResponse<PagedResult<RegistroAuditoriaDto>>.Ok(resultado));
    }
}
