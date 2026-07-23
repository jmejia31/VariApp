using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IJwtService _jwtService;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        IRolRepository rolRepository,
        IJwtService jwtService)
    {
        _usuarioRepository = usuarioRepository;
        _rolRepository = rolRepository;
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

        await AsegurarRolDinamicoAsync(usuario);

        if (usuario.RolEntidad!.Eliminado || !usuario.RolEntidad.Activo)
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

    private async Task AsegurarRolDinamicoAsync(InventoryApp.Domain.Entities.Usuario usuario)
    {
        if (usuario.RolId.HasValue && usuario.RolEntidad is not null)
            return;

        // Compatibilidad de una sola vía para usuarios creados antes del catálogo
        // dinámico. Se vinculan por el enum legado, pero desde este momento la
        // fuente de verdad pasa a ser Rol.Id y nunca vuelve a consultarse una
        // matriz legacy para autorizar solicitudes.
        var nombreRolSistema = usuario.Rol == RolUsuario.Administrador
            ? "Administrador"
            : "Vendedor";

        var rol = (await _rolRepository.GetAllAsync())
            .FirstOrDefault(r =>
                string.Equals(r.Nombre, nombreRolSistema, StringComparison.OrdinalIgnoreCase) &&
                !r.Eliminado);

        if (rol is null)
            throw new BusinessRuleException("La cuenta no tiene un rol dinámico válido asignado. Contacta a un administrador.");

        usuario.RolId = rol.Id;
        usuario.RolEntidad = rol;
        usuario.ActualizadoPorUsuarioId = usuario.Id;
        usuario.FechaActualizacion = DateTime.UtcNow;

        _usuarioRepository.Update(usuario);
        await _usuarioRepository.SaveChangesAsync();
    }
}
