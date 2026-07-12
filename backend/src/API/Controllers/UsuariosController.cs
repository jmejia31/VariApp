using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

/// Gestión de usuarios del sistema. Cada endpoint valida la acción exacta contra
/// la matriz de permisos (sección 10) — nunca solo [Authorize] genérico.
[ApiController]
[Authorize]
[Route("usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll()
    {
        var usuarios = await _usuarioService.GetAllAsync();
        return Ok(ApiResponse<List<UsuarioDto>>.Ok(usuarios));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Crear)]
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

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUsuarioDto dto)
    {
        var actualizado = await _usuarioService.UpdateAsync(id, dto);
        if (actualizado is null)
            return NotFound(ApiResponse<object>.Fail("Usuario no encontrado."));

        return Ok(ApiResponse<UsuarioDto>.Ok(actualizado, "Usuario actualizado correctamente."));
    }

    [HttpPut("{id:int}/estado")]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.CambiarEstado)]
    public async Task<IActionResult> UpdateEstado(int id, [FromBody] UpdateUsuarioEstadoDto dto)
    {
        var actualizado = await _usuarioService.UpdateEstadoAsync(id, dto.Activo);
        if (actualizado is null)
            return NotFound(ApiResponse<object>.Fail("Usuario no encontrado."));

        var accion = dto.Activo ? "activado" : "desactivado";
        return Ok(ApiResponse<UsuarioDto>.Ok(actualizado, $"Usuario {accion} correctamente."));
    }
}
