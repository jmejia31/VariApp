using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

/// Gestión de usuarios del sistema. Solo el rol Administrador puede administrar usuarios.
[ApiController]
[Authorize(Roles = "Administrador")]
[Route("usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var usuarios = await _usuarioService.GetAllAsync();
        return Ok(ApiResponse<List<UsuarioDto>>.Ok(usuarios));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUsuarioDto dto)
    {
        try
        {
            var creado = await _usuarioService.CreateAsync(dto);
            return Ok(ApiResponse<UsuarioDto>.Ok(creado, "Usuario creado correctamente."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> UpdateEstado(int id, [FromBody] UpdateUsuarioEstadoDto dto)
    {
        var actualizado = await _usuarioService.UpdateEstadoAsync(id, dto.Activo);
        if (actualizado is null)
            return NotFound(ApiResponse<object>.Fail("Usuario no encontrado."));

        var accion = dto.Activo ? "activado" : "desactivado";
        return Ok(ApiResponse<UsuarioDto>.Ok(actualizado, $"Usuario {accion} correctamente."));
    }
}
