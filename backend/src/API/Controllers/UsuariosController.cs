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
[Route("usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly IPermisoService _permisoService;
    private readonly ICurrentUserService _currentUser;

    public UsuariosController(
        IUsuarioService usuarioService,
        IPermisoService permisoService,
        ICurrentUserService currentUser)
    {
        _usuarioService = usuarioService;
        _permisoService = permisoService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll()
    {
        var usuarios = await _usuarioService.GetAllAsync();
        return Ok(ApiResponse<List<UsuarioDto>>.Ok(usuarios));
    }

    [HttpGet("paginado")]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Ver)]
    public async Task<IActionResult> GetPaged([FromQuery] PagedRequest request)
    {
        var resultado = await _usuarioService.GetPagedAsync(request);
        return Ok(ApiResponse<PagedResult<UsuarioDto>>.Ok(resultado));
    }

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
        await _permisoService.VerificarPermisoAsync(ModuloSistema.Usuarios, AccionPermiso.AsignarRol);

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
        var actual = await _usuarioService.GetByIdAsync(id);
        if (actual is null)
            return NotFound(ApiResponse<object>.Fail("Usuario no encontrado."));

        var cambiaRol = dto.RolId.HasValue
            ? actual.RolId != dto.RolId
            : !string.IsNullOrWhiteSpace(dto.Rol) &&
              !string.Equals(actual.Rol, dto.Rol, StringComparison.OrdinalIgnoreCase);

        if (cambiaRol)
            await _permisoService.VerificarPermisoAsync(ModuloSistema.Usuarios, AccionPermiso.AsignarRol);

        if (!string.IsNullOrWhiteSpace(dto.NuevaPassword))
            await _permisoService.VerificarPermisoAsync(ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena);

        var actualizado = await _usuarioService.UpdateAsync(id, dto);
        if (actualizado is null)
            return NotFound(ApiResponse<object>.Fail("Usuario no encontrado."));

        return Ok(ApiResponse<UsuarioDto>.Ok(actualizado, "Usuario actualizado correctamente."));
    }

    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> UpdateEstado(int id, [FromBody] UpdateUsuarioEstadoDto dto)
    {
        if (!dto.Activo && _currentUser.UsuarioId == id)
            return BadRequest(ApiResponse<object>.Fail("No puedes desactivar tu propia cuenta."));

        await _permisoService.VerificarPermisoAsync(
            ModuloSistema.Usuarios,
            dto.Activo ? AccionPermiso.Activar : AccionPermiso.Desactivar);

        var actualizado = await _usuarioService.UpdateEstadoAsync(id, dto.Activo);
        if (actualizado is null)
            return NotFound(ApiResponse<object>.Fail("Usuario no encontrado."));

        var accion = dto.Activo ? "activado" : "desactivado";
        return Ok(ApiResponse<UsuarioDto>.Ok(actualizado, $"Usuario {accion} correctamente."));
    }

    [HttpPut("{id:int}/bloquear")]
    public async Task<IActionResult> Bloquear(int id, [FromBody] BloquearUsuarioDto dto)
    {
        await _permisoService.VerificarPermisoAsync(ModuloSistema.Usuarios, AccionPermiso.Desactivar);
        var actualizado = await _usuarioService.BloquearAsync(id, dto.Motivo);
        return Ok(ApiResponse<UsuarioDto>.Ok(actualizado, "Usuario bloqueado correctamente."));
    }

    [HttpPut("{id:int}/desbloquear")]
    public async Task<IActionResult> Desbloquear(int id)
    {
        await _permisoService.VerificarPermisoAsync(ModuloSistema.Usuarios, AccionPermiso.Activar);
        var actualizado = await _usuarioService.DesbloquearAsync(id);
        return Ok(ApiResponse<UsuarioDto>.Ok(actualizado, "Usuario desbloqueado correctamente."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _usuarioService.EliminarAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "Usuario eliminado correctamente."));
    }
}
