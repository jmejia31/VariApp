using InventoryApp.Application.Common;
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
    private readonly ICurrentUserService _currentUser;

    public UsuarioService(IUsuarioRepository repository, IRolRepository rolRepository, IAuditoriaService auditoria, ICurrentUserService currentUser)
    {
        _repository = repository;
        _rolRepository = rolRepository;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<List<UsuarioDto>> GetAllAsync()
    {
        var usuarios = await _repository.GetAllAsync();
        return usuarios.Select(ToDto).ToList();
    }

    public async Task<PagedResult<UsuarioDto>> GetPagedAsync(PagedRequest request)
    {
        var resultado = await _repository.GetPagedAsync(request);
        return new PagedResult<UsuarioDto>
        {
            Items = resultado.Items.Select(ToDto).ToList(),
            Page = resultado.Page,
            PageSize = resultado.PageSize,
            TotalCount = resultado.TotalCount
        };
    }

    public async Task<UsuarioDetalleDto?> GetByIdAsync(int id)
    {
        var usuario = await _repository.GetByIdAsync(id);
        return usuario is null ? null : ToDetalleDto(usuario);
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

        if (rolDinamico is not null)
            rolLegado = rolDinamico.EsAdministrador ? RolUsuario.Administrador : RolUsuario.Vendedor;

        var usuario = new Usuario
        {
            NombreUsuario = dto.NombreUsuario.Trim(),
            NombreCompleto = dto.NombreCompleto.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Rol = rolLegado,
            RolId = rolDinamico?.Id,
            Activo = true,
            CreadoPorUsuarioId = _currentUser.UsuarioId
        };

        await _repository.AddAsync(usuario);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Usuarios, AccionPermiso.Crear,
            $"Creó el usuario '{usuario.NombreUsuario}' con rol '{(rolDinamico?.Nombre ?? rolLegado.ToString())}'.", usuario.Id,
            entidad: "Usuario", valoresNuevos: new { usuario.NombreUsuario, usuario.NombreCompleto, Rol = rolDinamico?.Nombre ?? rolLegado.ToString() });

        return ToDto(usuario);
    }

    public async Task<UsuarioDto?> UpdateAsync(int id, UpdateUsuarioDto dto)
    {
        var usuario = await _repository.GetByIdAsync(id);
        if (usuario is null) return null;

        var rolAnterior = usuario.RolId?.ToString() ?? usuario.Rol.ToString();
        var rolDinamico = await ResolverRolDinamicoAsync(dto.RolId);

        // No permitir que un administrador se quite a sí mismo el último acceso
        // administrativo (sección 7: "un administrador no debe eliminar
        // accidentalmente su propio acceso" / "no debe eliminarse el último rol
        // con administración completa").
        var eraAdmin = usuario.RolEntidad?.EsAdministrador ?? (usuario.Rol == RolUsuario.Administrador);
        var seraAdmin = rolDinamico?.EsAdministrador ?? eraAdmin;
        if (eraAdmin && !seraAdmin && _currentUser.UsuarioId == usuario.Id)
        {
            var otrosAdmins = await _repository.ContarAdministradoresActivosAsync(excluirUsuarioId: usuario.Id);
            if (otrosAdmins == 0)
                throw new BusinessRuleException("No puedes quitarte a ti mismo el rol de administrador: eres el único administrador activo del sistema.");
        }

        usuario.NombreCompleto = dto.NombreCompleto.Trim();
        usuario.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        usuario.FechaActualizacion = DateTime.UtcNow;

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
                $"Cambió el rol del usuario '{usuario.NombreUsuario}' de '{rolAnterior}' a '{rolNuevo}'.", usuario.Id,
                entidad: "Usuario", valoresAnteriores: new { Rol = rolAnterior }, valoresNuevos: new { Rol = rolNuevo });
        }
        else
        {
            await _auditoria.RegistrarAsync(ModuloSistema.Usuarios, AccionPermiso.Editar,
                $"Editó el usuario '{usuario.NombreUsuario}'.", usuario.Id, entidad: "Usuario");
        }

        if (huboResetPassword)
        {
            await _auditoria.RegistrarAsync(ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena,
                $"Restableció la contraseña del usuario '{usuario.NombreUsuario}'.", usuario.Id, entidad: "Usuario");
        }

        return ToDto(usuario);
    }

    public async Task<UsuarioDto?> UpdateEstadoAsync(int id, bool activo)
    {
        var usuario = await _repository.GetByIdAsync(id);
        if (usuario is null) return null;

        if (!activo)
            await ValidarNoEsUltimoAdminAsync(usuario, "desactivar");

        usuario.Activo = activo;
        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Usuarios,
            activo ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"{(activo ? "Activó" : "Desactivó")} el usuario '{usuario.NombreUsuario}'.", usuario.Id, entidad: "Usuario");

        return ToDto(usuario);
    }

    public async Task<UsuarioDto> BloquearAsync(int id, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessRuleException("El motivo del bloqueo es obligatorio.");

        var usuario = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El usuario no existe.");

        if (usuario.Id == _currentUser.UsuarioId)
            throw new BusinessRuleException("No puedes bloquearte a ti mismo.");

        await ValidarNoEsUltimoAdminAsync(usuario, "bloquear");

        usuario.Bloqueado = true;
        usuario.MotivoBloqueo = motivo.Trim();
        usuario.FechaBloqueo = DateTime.UtcNow;
        usuario.BloqueadoPorUsuarioId = _currentUser.UsuarioId;
        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Usuarios, AccionPermiso.Desactivar,
            $"Bloqueó al usuario '{usuario.NombreUsuario}'.", usuario.Id, entidad: "Usuario", motivo: motivo);

        return ToDto(usuario);
    }

    public async Task<UsuarioDto> DesbloquearAsync(int id)
    {
        var usuario = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El usuario no existe.");

        usuario.Bloqueado = false;
        usuario.MotivoBloqueo = null;
        usuario.FechaBloqueo = null;
        usuario.BloqueadoPorUsuarioId = null;
        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Usuarios, AccionPermiso.Activar,
            $"Desbloqueó al usuario '{usuario.NombreUsuario}'.", usuario.Id, entidad: "Usuario");

        return ToDto(usuario);
    }

    public async Task EliminarAsync(int id)
    {
        var usuario = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El usuario no existe.");

        if (usuario.Id == _currentUser.UsuarioId)
            throw new BusinessRuleException("No puedes eliminar tu propia cuenta.");

        await ValidarNoEsUltimoAdminAsync(usuario, "eliminar");

        // Eliminación lógica (sección 9/14): un usuario puede tener ventas,
        // compras, movimientos y auditoría asociados por FK — nunca se elimina
        // físicamente, se conserva el registro con Eliminado=true.
        usuario.Eliminado = true;
        usuario.Activo = false;
        usuario.FechaEliminacion = DateTime.UtcNow;
        usuario.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Usuarios, AccionPermiso.EliminarLogico,
            $"Eliminó (lógico) al usuario '{usuario.NombreUsuario}'.", usuario.Id, entidad: "Usuario");
    }

    /// Regla de seguridad compartida (sección 7 del prompt): nunca desactivar,
    /// bloquear o eliminar el último usuario administrador activo del sistema.
    private async Task ValidarNoEsUltimoAdminAsync(Usuario usuario, string accion)
    {
        var esAdmin = usuario.RolEntidad?.EsAdministrador ?? (usuario.Rol == RolUsuario.Administrador);
        if (!esAdmin) return;

        var otrosAdmins = await _repository.ContarAdministradoresActivosAsync(excluirUsuarioId: usuario.Id);
        if (otrosAdmins == 0)
            throw new BusinessRuleException($"No se puede {accion} este usuario: es el único administrador activo del sistema.");
    }

    private static UsuarioDto ToDto(Usuario u) => new()
    {
        Id = u.Id,
        NombreUsuario = u.NombreUsuario,
        NombreCompleto = u.NombreCompleto,
        Rol = u.Rol.ToString(),
        RolId = u.RolId,
        Activo = u.Activo,
        Bloqueado = u.Bloqueado,
        FechaCreacion = u.FechaCreacion
    };

    private static UsuarioDetalleDto ToDetalleDto(Usuario u) => new()
    {
        Id = u.Id,
        NombreUsuario = u.NombreUsuario,
        NombreCompleto = u.NombreCompleto,
        Rol = u.Rol.ToString(),
        RolId = u.RolId,
        RolNombre = u.RolEntidad?.Nombre,
        Activo = u.Activo,
        Bloqueado = u.Bloqueado,
        MotivoBloqueo = u.MotivoBloqueo,
        FechaBloqueo = u.FechaBloqueo,
        FechaCreacion = u.FechaCreacion,
        FechaActualizacion = u.FechaActualizacion
    };
}
