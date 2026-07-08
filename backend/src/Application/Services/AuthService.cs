using InventoryApp.Application.DTOs;
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
        if (usuario is null) return null;

        var passwordValida = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);
        if (!passwordValida) return null;

        var (token, expiraEn) = _jwtService.GenerarToken(usuario);

        return new LoginResponseDto
        {
            Token = token,
            NombreUsuario = usuario.NombreUsuario,
            ExpiraEn = expiraEn
        };
    }
}
