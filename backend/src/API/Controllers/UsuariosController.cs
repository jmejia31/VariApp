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

    /// Listado paginado con búsqueda (sección 4: "buscar, filtrar, ordenar,
    /// paginar"). GetAll se conserva para no romper consumidores existentes
    /// (ej. el selector de usuarios en otros formularios).
    [HttpGet("paginado")]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Ver)]
    public async Task<IActionResult> GetPaged([FromQuery] PagedRequest request)
    {
        var resultado = await _usuarioService.GetPagedAsync(request);
        return Ok(ApiResponse<PagedResult<UsuarioDto>>.Ok(resultado));
    }

    /// Vista de detalle real, separada del formulario de edición (sección 4:
    /// "no reutilices incorrectamente el formulario de edición como vista de consulta").
    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var usuario = await _usuarioService.GetByIdAsync(id);
        if (usuario is null)
            return NotFound(ApiResponse<object>.Fail("Usuario no encontrado."));

        return Ok(ApiResponse<UsuarioDetalleDto>.Ok(usuario));
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

    /// Bloqueo: distinto de desactivar (sección 4). Requiere motivo explícito.
    [HttpPut("{id:int}/bloquear")]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.CambiarEstado)]
    public async Task<IActionResult> Bloquear(int id, [FromBody] BloquearUsuarioDto dto)
    {
        var actualizado = await _usuarioService.BloquearAsync(id, dto.Motivo);
        return Ok(ApiResponse<UsuarioDto>.Ok(actualizado, "Usuario bloqueado correctamente."));
    }

    [HttpPut("{id:int}/desbloquear")]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.CambiarEstado)]
    public async Task<IActionResult> Desbloquear(int id)
    {
        var actualizado = await _usuarioService.DesbloquearAsync(id);
        return Ok(ApiResponse<UsuarioDto>.Ok(actualizado, "Usuario desbloqueado correctamente."));
    }

    /// Eliminación lógica (sección 9/14): nunca física, un usuario puede tener
    /// ventas/compras/auditoría asociados por FK.
    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _usuarioService.EliminarAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "Usuario eliminado correctamente."));
    }
}
