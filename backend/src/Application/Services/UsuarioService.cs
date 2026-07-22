using System.Text.RegularExpressions;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class UsuarioService : IUsuarioService
{
    private static readonly Regex NombreUsuarioValido = new(
        "^[a-zA-Z0-9._-]{3,50}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IUsuarioRepository _repository;
    private readonly IRolRepository _rolRepository;
    private readonly IAuditoriaService _auditoria;
    private readonly ICurrentUserService _currentUser;

    public UsuarioService(
        IUsuarioRepository repository,
        IRolRepository rolRepository,
        IAuditoriaService auditoria,
        ICurrentUserService currentUser)
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
        var nombreUsuario = NormalizarNombreUsuario(dto.NombreUsuario);
        var nombreCompleto = NormalizarNombreCompleto(dto.NombreCompleto);
        ValidarPasswordSegura(dto.Password);

        var existente = await _repository.GetByNombreUsuarioAsync(nombreUsuario);
        if (existente is not null)
            throw new BusinessRuleException("Ya existe un usuario con ese nombre de usuario.");

        var rolDinamico = await ResolverRolDinamicoAsync(dto.RolId);
        if (!Enum.TryParse<RolUsuario>(dto.Rol, out var rolLegado))
            rolLegado = RolUsuario.Vendedor;

        if (rolDinamico is not null)
            rolLegado = rolDinamico.EsAdministrador ? RolUsuario.Administrador : RolUsuario.Vendedor;

        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario,
            NombreCompleto = nombreCompleto,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
            Rol = rolLegado,
            RolId = rolDinamico?.Id,
            Activo = true,
            CreadoPorUsuarioId = _currentUser.UsuarioId
        };

        await _repository.AddAsync(usuario);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.Crear,
            $"Creó el usuario '{usuario.NombreUsuario}' con rol '{(rolDinamico?.Nombre ?? rolLegado.ToString())}'.",
            usuario.Id,
            entidad: "Usuario",
            valoresNuevos: new
            {
                usuario.NombreUsuario,
                usuario.NombreCompleto,
                Rol = rolDinamico?.Nombre ?? rolLegado.ToString()
            });

        return ToDto(usuario);
    }

    public async Task<UsuarioDto?> UpdateAsync(int id, UpdateUsuarioDto dto)
    {
        var usuario = await _repository.GetByIdAsync(id);
        if (usuario is null) return null;

        var nombreUsuario = NormalizarNombreUsuario(dto.NombreUsuario);
        var nombreCompleto = NormalizarNombreCompleto(dto.NombreCompleto);
        var existente = await _repository.GetByNombreUsuarioAsync(nombreUsuario);
        if (existente is not null && existente.Id != usuario.Id)
            throw new BusinessRuleException("Ya existe otro usuario con ese nombre de usuario.");

        var datosAnteriores = new
        {
            usuario.NombreUsuario,
            usuario.NombreCompleto,
            Rol = usuario.RolEntidad?.Nombre ?? usuario.Rol.ToString()
        };

        var rolDinamico = await ResolverRolDinamicoAsync(dto.RolId);
        var eraAdmin = usuario.RolEntidad?.EsAdministrador ?? usuario.Rol == RolUsuario.Administrador;
        var seraAdmin = rolDinamico?.EsAdministrador ?? eraAdmin;

        if (eraAdmin && !seraAdmin && _currentUser.UsuarioId == usuario.Id)
        {
            var otrosAdmins = await _repository.ContarAdministradoresActivosAsync(excluirUsuarioId: usuario.Id);
            if (otrosAdmins == 0)
                throw new BusinessRuleException("No puedes quitarte a ti mismo el rol de administrador: eres el único administrador activo del sistema.");
        }

        usuario.NombreUsuario = nombreUsuario;
        usuario.NombreCompleto = nombreCompleto;
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
        {
            ValidarPasswordSegura(dto.NuevaPassword!);
            if (BCrypt.Net.BCrypt.Verify(dto.NuevaPassword!, usuario.PasswordHash))
                throw new BusinessRuleException("La nueva contraseña debe ser diferente de la actual.");

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword!, workFactor: 12);
        }

        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        var rolNuevo = usuario.RolEntidad?.Nombre ?? rolDinamico?.Nombre ?? usuario.Rol.ToString();
        if (!string.Equals(datosAnteriores.Rol, rolNuevo, StringComparison.Ordinal))
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.Usuarios,
                AccionPermiso.AsignarRol,
                $"Cambió el rol del usuario '{usuario.NombreUsuario}' de '{datosAnteriores.Rol}' a '{rolNuevo}'.",
                usuario.Id,
                entidad: "Usuario",
                valoresAnteriores: new { datosAnteriores.Rol },
                valoresNuevos: new { Rol = rolNuevo });
        }

        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.Editar,
            $"Editó los datos del usuario '{usuario.NombreUsuario}'.",
            usuario.Id,
            entidad: "Usuario",
            valoresAnteriores: new { datosAnteriores.NombreUsuario, datosAnteriores.NombreCompleto },
            valoresNuevos: new { usuario.NombreUsuario, usuario.NombreCompleto });

        if (huboResetPassword)
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.Usuarios,
                AccionPermiso.RestablecerContrasena,
                $"Restableció la contraseña del usuario '{usuario.NombreUsuario}'.",
                usuario.Id,
                entidad: "Usuario");
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
        usuario.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        usuario.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            activo ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"{(activo ? "Activó" : "Desactivó")} el usuario '{usuario.NombreUsuario}'.",
            usuario.Id,
            entidad: "Usuario");

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
        usuario.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        usuario.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.Desactivar,
            $"Bloqueó al usuario '{usuario.NombreUsuario}'.",
            usuario.Id,
            entidad: "Usuario",
            motivo: motivo);

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
        usuario.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        usuario.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.Activar,
            $"Desbloqueó al usuario '{usuario.NombreUsuario}'.",
            usuario.Id,
            entidad: "Usuario");

        return ToDto(usuario);
    }

    public async Task EliminarAsync(int id)
    {
        var usuario = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El usuario no existe.");

        if (usuario.Id == _currentUser.UsuarioId)
            throw new BusinessRuleException("No puedes eliminar tu propia cuenta.");

        await ValidarNoEsUltimoAdminAsync(usuario, "eliminar");

        usuario.Eliminado = true;
        usuario.Activo = false;
        usuario.FechaEliminacion = DateTime.UtcNow;
        usuario.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        usuario.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        usuario.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.EliminarLogico,
            $"Eliminó lógicamente al usuario '{usuario.NombreUsuario}'.",
            usuario.Id,
            entidad: "Usuario");
    }

    private async Task ValidarNoEsUltimoAdminAsync(Usuario usuario, string accion)
    {
        var esAdmin = usuario.RolEntidad?.EsAdministrador ?? usuario.Rol == RolUsuario.Administrador;
        if (!esAdmin) return;

        var otrosAdmins = await _repository.ContarAdministradoresActivosAsync(excluirUsuarioId: usuario.Id);
        if (otrosAdmins == 0)
            throw new BusinessRuleException($"No se puede {accion} este usuario: es el único administrador activo del sistema.");
    }

    private static string NormalizarNombreUsuario(string valor)
    {
        var normalizado = valor?.Trim() ?? string.Empty;
        if (!NombreUsuarioValido.IsMatch(normalizado))
            throw new BusinessRuleException(
                "El nombre de usuario debe tener entre 3 y 50 caracteres y usar únicamente letras, números, punto, guion o guion bajo.");

        return normalizado;
    }

    private static string NormalizarNombreCompleto(string valor)
    {
        var normalizado = Regex.Replace(valor?.Trim() ?? string.Empty, "\\s+", " ");
        if (normalizado.Length < 3 || normalizado.Length > 150)
            throw new BusinessRuleException("El nombre completo debe tener entre 3 y 150 caracteres.");

        return normalizado;
    }

    private static void ValidarPasswordSegura(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 10 || password.Length > 128)
            throw new BusinessRuleException("La contraseña debe tener entre 10 y 128 caracteres.");

        if (!password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) ||
            !password.Any(c => !char.IsLetterOrDigit(c)))
        {
            throw new BusinessRuleException("La contraseña debe incluir mayúscula, minúscula, número y símbolo.");
        }
    }

    private static UsuarioDto ToDto(Usuario u) => new()
    {
        Id = u.Id,
        NombreUsuario = u.NombreUsuario,
        NombreCompleto = u.NombreCompleto,
        Rol = u.RolEntidad?.Nombre ?? u.Rol.ToString(),
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
        Rol = u.RolEntidad?.Nombre ?? u.Rol.ToString(),
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
