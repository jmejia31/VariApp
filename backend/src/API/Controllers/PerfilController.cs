using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

/// Autogestión del usuario autenticado (ver su perfil, editar su nombre,
/// cambiar su propia contraseña). Deliberadamente solo requiere [Authorize]
/// — no un permiso de módulo — porque cualquier usuario válido puede
/// gestionar su propia cuenta, sin importar su rol.
[ApiController]
[Authorize]
[Route("perfil")]
public class PerfilController : ControllerBase
{
    private readonly IPerfilService _service;

    public PerfilController(IPerfilService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var perfil = await _service.GetPerfilAsync();
        return Ok(ApiResponse<PerfilDto>.Ok(perfil));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ActualizarPerfilDto dto)
    {
        var actualizado = await _service.ActualizarPerfilAsync(dto);
        return Ok(ApiResponse<PerfilDto>.Ok(actualizado, "Perfil actualizado correctamente."));
    }

    [HttpPut("password")]
    public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordPropiaDto dto)
    {
        await _service.CambiarPasswordAsync(dto);
        return Ok(ApiResponse<object>.Ok(new { }, "Contraseña actualizada correctamente."));
    }
}
