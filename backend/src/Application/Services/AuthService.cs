using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;

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
        var nombreUsuario = dto.NombreUsuario?.Trim() ?? string.Empty;
        var usuario = await _usuarioRepository.GetByNombreUsuarioAsync(nombreUsuario);
        if (usuario is null || usuario.Eliminado) return null;

        var passwordValida = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);
        if (!passwordValida) return null;

        if (usuario.Bloqueado)
            throw new BusinessRuleException($"Esta cuenta está bloqueada. Motivo: {usuario.MotivoBloqueo ?? "no especificado"}. Contacta a un administrador.");
        if (!usuario.Activo)
            throw new BusinessRuleException("Esta cuenta está desactivada. Contacta a un administrador.");

        // Rol.Id es la fuente de verdad. Nunca se emite una sesión apoyada solo
        // en el enum legado, porque eso puede cargar una matriz distinta a la
        // administrada y mostrar módulos no autorizados.
        if (!usuario.RolId.HasValue || usuario.RolEntidad is null)
            throw new BusinessRuleException("La cuenta no tiene un rol válido asignado. Contacta a un administrador.");
        if (usuario.RolEntidad.Eliminado || !usuario.RolEntidad.Activo)
            throw new BusinessRuleException("El rol asignado a esta cuenta está inactivo. Contacta a un administrador.");

        var (token, expiraEn) = _jwtService.GenerarToken(usuario);

        return new LoginResponseDto
        {
            Token = token,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompleto = usuario.NombreCompleto,
            Rol = usuario.RolEntidad.Nombre,
            FotoPerfilUrl = usuario.FotoPerfilUrl,
            ExpiraEn = expiraEn
        };
    }
}
