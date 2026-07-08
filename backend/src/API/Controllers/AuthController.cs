using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var resultado = await _authService.LoginAsync(dto);

        if (resultado is null)
            return Unauthorized(ApiResponse<object>.Fail("Usuario o contraseña incorrectos."));

        return Ok(ApiResponse<LoginResponseDto>.Ok(resultado, "Login exitoso."));
    }
}
