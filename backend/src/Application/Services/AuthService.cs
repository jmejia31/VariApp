using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using BCrypt.Net;

namespace InventoryApp.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IJwtService _jwtService;

    public AuthService(IUsuarioRepository usuarioRepository, IJwtService jwtService)
    {
        _usuarioRepository = usuarioRepository;
        _jwtService = jwtService;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto)
    {
        var usuario = await _usuarioRepository.GetByNombreUsuarioAsync(dto.NombreUsuario);
        if (usuario is null || usuario.Eliminado) return null;

        var passwordValida = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);
        if (!passwordValida) return null;

        // A diferencia de credenciales inválidas (mensaje genérico por seguridad),
        // el bloqueo/desactivación sí se comunica explícitamente: la contraseña
        // era correcta, el dueño legítimo de la cuenta necesita saber por qué no entra.
        if (usuario.Bloqueado)
            throw new BusinessRuleException($"Esta cuenta está bloqueada. Motivo: {usuario.MotivoBloqueo ?? "no especificado"}. Contacta a un administrador.");
        if (!usuario.Activo)
            throw new BusinessRuleException("Esta cuenta está desactivada. Contacta a un administrador.");

        var (token, expiraEn) = _jwtService.GenerarToken(usuario);

        return new LoginResponseDto
        {
            Token = token,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompleto = usuario.NombreCompleto,
            Rol = usuario.Rol.ToString(),
            ExpiraEn = expiraEn
        };
    }
}
