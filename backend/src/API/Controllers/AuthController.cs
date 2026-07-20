using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuditoriaService _auditoria;

    public AuthController(IAuthService authService, IAuditoriaService auditoria)
    {
        _authService = authService;
        _auditoria = auditoria;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var resultado = await _authService.LoginAsync(dto);

        if (resultado is null)
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.Usuarios,
                AccionPermiso.ConsultarHistorial,
                $"Intento fallido de inicio de sesion para '{dto.NombreUsuario}'.",
                entidad: "Sesion",
                resultado: "Rechazado",
                error: "Credenciales invalidas");

            return Unauthorized(ApiResponse<object>.Fail("Usuario o contrasena incorrectos."));
        }

        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.ConsultarHistorial,
            $"Inicio de sesion exitoso para '{resultado.NombreUsuario}'.",
            entidad: "Sesion",
            resultado: "Exito");

        return Ok(ApiResponse<LoginResponseDto>.Ok(resultado, "Login exitoso."));
    }
}
