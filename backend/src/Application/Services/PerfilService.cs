using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class PerfilService : IPerfilService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;

    public PerfilService(IUsuarioRepository usuarioRepository, ICurrentUserService currentUser, IAuditoriaService auditoria)
    {
        _usuarioRepository = usuarioRepository;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    private async Task<Domain.Entities.Usuario> GetUsuarioActualAsync()
    {
        var id = _currentUser.UsuarioId
            ?? throw new BusinessRuleException("No se pudo identificar al usuario autenticado.");

        return await _usuarioRepository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El usuario autenticado ya no existe.");
    }

    public async Task<PerfilDto> GetPerfilAsync()
    {
        var usuario = await GetUsuarioActualAsync();
        return ToDto(usuario);
    }

    public async Task<PerfilDto> ActualizarPerfilAsync(ActualizarPerfilDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreCompleto))
            throw new BusinessRuleException("El nombre completo es obligatorio.");

        var usuario = await GetUsuarioActualAsync();
        var anterior = usuario.NombreCompleto;

        usuario.NombreCompleto = dto.NombreCompleto.Trim();
        _usuarioRepository.Update(usuario);
        await _usuarioRepository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Usuarios, AccionPermiso.Editar,
            $"El usuario '{usuario.NombreUsuario}' actualizó su propio perfil.", usuario.Id,
            entidad: "Usuario", valoresAnteriores: new { NombreCompleto = anterior },
            valoresNuevos: new { usuario.NombreCompleto });

        return ToDto(usuario);
    }

    public async Task CambiarPasswordAsync(CambiarPasswordPropiaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PasswordNueva) || dto.PasswordNueva.Length < 8)
            throw new BusinessRuleException("La nueva contraseña debe tener al menos 8 caracteres.");

        var usuario = await GetUsuarioActualAsync();

        if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash))
            throw new BusinessRuleException("La contraseña actual no es correcta.");

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNueva);
        _usuarioRepository.Update(usuario);
        await _usuarioRepository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena,
            $"El usuario '{usuario.NombreUsuario}' cambió su propia contraseña.", usuario.Id, entidad: "Usuario");
    }

    private static PerfilDto ToDto(Domain.Entities.Usuario u) => new()
    {
        Id = u.Id,
        NombreUsuario = u.NombreUsuario,
        NombreCompleto = u.NombreCompleto,
        Rol = u.Rol.ToString(),
        FechaCreacion = u.FechaCreacion
    };
}
