using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("permisos")]
public class PermisosController : ControllerBase
{
    private readonly IPermisoService _service;

    public PermisosController(IPermisoService service)
    {
        _service = service;
    }

    [HttpGet("matriz")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> GetMatriz()
    {
        var matriz = await _service.GetMatrizAsync();
        return Ok(ApiResponse<List<PermisoMatrizItemDto>>.Ok(matriz));
    }

    [HttpPut("matriz")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> UpdateMatriz([FromBody] UpdatePermisoMatrizDto dto)
    {
        var actualizada = await _service.UpdateMatrizAsync(dto);
        return Ok(ApiResponse<List<PermisoMatrizItemDto>>.Ok(actualizada, "Matriz de permisos actualizada correctamente."));
    }

    [HttpGet("mis-permisos")]
    public async Task<IActionResult> GetMisPermisos()
    {
        var permisos = await _service.GetMisPermisosAsync();
        return Ok(ApiResponse<MisPermisosDto>.Ok(permisos));
    }
}
