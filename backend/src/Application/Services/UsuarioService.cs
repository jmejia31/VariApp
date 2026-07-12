using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _repository;
    private readonly IRolRepository _rolRepository;
    private readonly IAuditoriaService _auditoria;

    public UsuarioService(IUsuarioRepository repository, IRolRepository rolRepository, IAuditoriaService auditoria)
    {
        _repository = repository;
        _rolRepository = rolRepository;
        _auditoria = auditoria;
    }

    public async Task<List<UsuarioDto>> GetAllAsync()
    {
        var usuarios = await _repository.GetAllAsync();
        return usuarios.Select(ToDto).ToList();
    }

    /// Resuelve y valida el rol dinámico solicitado (si se envió RolId). El backend
    /// nunca confía ciegamente en el rol enviado desde Angular: valida que exista,
    /// que esté activo y no eliminado (sección 4: "Los roles inactivos no pueden
    /// asignarse" / "Los roles eliminados lógicamente no deben aparecer en nuevas
    /// asignaciones").
    private async Task<Rol?> ResolverRolDinamicoAsync(int? rolId)
    {
        if (!rolId.HasValue) return null;

        var rol = await _rolRepository.GetByIdAsync(rolId.Value)
            ?? throw new BusinessRuleException("El rol seleccionado no existe.");

        if (!rol.Activo)
            throw new BusinessRuleException("No se puede asignar un rol inactivo.");

        return rol;
    }

    public async Task<UsuarioDto> CreateAsync(CreateUsuarioDto dto)
    {
        var existente = await _repository.GetByNombreUsuarioAsync(dto.NombreUsuario);
        if (existente is not null)
            throw new InvalidOperationException("Ya existe un usuario con ese nombre de usuario.");

        var rolDinamico = await ResolverRolDinamicoAsync(dto.RolId);

        if (!Enum.TryParse<RolUsuario>(dto.Rol, out var rolLegado))
            rolLegado = RolUsuario.Vendedor;

        // Si hay rol dinámico, éste manda sobre el enum legado (que se deriva de él
        // solo para mantener compatibilidad con [Authorize(Roles=...)] existente).
        if (rolDinamico is not null)
            rolLegado = rolDinamico.EsAdministrador ? RolUsuario.Administrador : RolUsuario.Vendedor;

        var usuario = new Usuario
        {
            NombreUsuario = dto.NombreUsuario.Trim(),
            NombreCompleto = dto.NombreCompleto.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Rol = rolLegado,
            RolId = rolDinamico?.Id,
            Activo = true
        };

        await _repository.AddAsync(usuario);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Usuarios, AccionPermiso.Crear,
            $"Creó el usuario '{usuario.NombreUsuario}' con rol '{(rolDinamico?.Nombre ?? rolLegado.ToString())}'.", usuario.Id);

        return ToDto(usuario);
    }

    public async Task<UsuarioDto?> UpdateAsync(int id, UpdateUsuarioDto dto)
    {
        var usuario = await _repository.GetByIdAsync(id);
        if (usuario is null) return null;

        var rolAnterior = usuario.RolId?.ToString() ?? usuario.Rol.ToString();
        var rolDinamico = await ResolverRolDinamicoAsync(dto.RolId);

        usuario.NombreCompleto = dto.NombreCompleto.Trim();

        if (rolDinamico is not null)
        {
            usuario.RolId = rolDinamico.Id;
            usuario.Rol = rolDinamico.EsAdministrador ? RolUsuario.Administrador : RolUsuario.Vendedor;
        }
        else if (Enum.TryParse<RolUsuario>(dto.Rol, out var rolLegado))
        {
            usuario.Rol = rolLegado;
        }

        var huboResetPassword = !string.IsNullOrWhiteSpace(dto.NuevaPassword);
        if (huboResetPassword)
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword!);

        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        var rolNuevo = usuario.RolId?.ToString() ?? usuario.Rol.ToString();
        if (rolAnterior != rolNuevo)
        {
            await _auditoria.RegistrarAsync(ModuloSistema.Usuarios, AccionPermiso.AsignarRol,
                $"Cambió el rol del usuario '{usuario.NombreUsuario}' de '{rolAnterior}' a '{rolNuevo}'.", usuario.Id);
        }
        else
        {
            await _auditoria.RegistrarAsync(ModuloSistema.Usuarios, AccionPermiso.Editar,
                $"Editó el usuario '{usuario.NombreUsuario}'.", usuario.Id);
        }

        if (huboResetPassword)
        {
            await _auditoria.RegistrarAsync(ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena,
                $"Restableció la contraseña del usuario '{usuario.NombreUsuario}'.", usuario.Id);
        }

        return ToDto(usuario);
    }

    public async Task<UsuarioDto?> UpdateEstadoAsync(int id, bool activo)
    {
        var usuario = await _repository.GetByIdAsync(id);
        if (usuario is null) return null;

        usuario.Activo = activo;
        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Usuarios,
            activo ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"{(activo ? "Activó" : "Desactivó")} el usuario '{usuario.NombreUsuario}'.", usuario.Id);

        return ToDto(usuario);
    }

    private static UsuarioDto ToDto(Usuario u) => new()
    {
        Id = u.Id,
        NombreUsuario = u.NombreUsuario,
        NombreCompleto = u.NombreCompleto,
        Rol = u.Rol.ToString(),
        RolId = u.RolId,
        Activo = u.Activo,
        FechaCreacion = u.FechaCreacion
    };
}
