using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class PermisoService : IPermisoService
{
    private readonly IRolPermisoRepository _repository;
    private readonly IRolRepository _rolRepository;
    private readonly IPermisoRepository _permisoRepository;
    private readonly IAuditoriaService _auditoria;
    private readonly ICurrentUserService _currentUser;
    private readonly IUsuarioScopeService _usuarioScope;

    public PermisoService(
        IRolPermisoRepository repository,
        IRolRepository rolRepository,
        IPermisoRepository permisoRepository,
        IAuditoriaService auditoria,
        ICurrentUserService currentUser,
        IUsuarioScopeService usuarioScope)
    {
        _repository = repository;
        _rolRepository = rolRepository;
        _permisoRepository = permisoRepository;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _usuarioScope = usuarioScope;
    }

    public async Task<List<PermisoMatrizItemDto>> GetMatrizAsync(int rolId)
    {
        var existentes = await _repository.GetByRolIdAsync(rolId);
        var resultado = new List<PermisoMatrizItemDto>();

        foreach (var (modulo, acciones) in CatalogoPermisosBase.Definicion)
        {
            foreach (var accion in acciones)
            {
                var fila = existentes.FirstOrDefault(p => p.Modulo == modulo && p.Accion == accion);
                resultado.Add(new PermisoMatrizItemDto
                {
                    Rol = rolId.ToString(),
                    Modulo = modulo.ToString(),
                    Accion = accion.ToString(),
                    Permitido = fila?.Permitido ?? false
                });
            }
        }

        return resultado;
    }

    public async Task<List<PermisoMatrizItemDto>> UpdateMatrizAsync(int rolId, UpdatePermisoMatrizDto dto)
    {
        var rol = await _rolRepository.GetByIdAsync(rolId)
            ?? throw new BusinessRuleException("El rol seleccionado no existe.");

        if (!rol.Activo)
            throw new BusinessRuleException("No se pueden asignar permisos a un rol inactivo.");

        var matrizAnterior = await GetMatrizAsync(rolId);
        var nuevaMatriz = new List<RolPermiso>();
        var claves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in dto.Permisos.Where(p => p.Permitido))
        {
            if (!Enum.TryParse<ModuloSistema>(item.Modulo, out var modulo) ||
                !Enum.TryParse<AccionPermiso>(item.Accion, out var accion))
                throw new BusinessRuleException($"Combinación inválida '{item.Modulo}.{item.Accion}'.");

            var valido = CatalogoPermisosBase.Definicion.Any(d => d.Modulo == modulo && d.Acciones.Contains(accion));
            if (!valido)
                throw new BusinessRuleException($"La acción '{accion}' no aplica al módulo '{modulo}'.");

            var clave = $"{modulo}:{accion}";
            if (!claves.Add(clave))
                throw new BusinessRuleException($"El permiso '{clave}' viene duplicado en la solicitud.");

            var permiso = await _permisoRepository.GetByModuloAccionAsync(modulo, accion)
                ?? throw new BusinessRuleException($"El permiso '{clave}' no existe, está inactivo o fue eliminado.");

            nuevaMatriz.Add(new RolPermiso
            {
                Rol = rol.EsAdministrador ? RolUsuario.Administrador : RolUsuario.Vendedor,
                RolId = rolId,
                PermisoId = permiso.Id,
                Modulo = modulo,
                Accion = accion,
                Permitido = true
            });
        }

        await _repository.ReemplazarMatrizPorRolIdAsync(rolId, nuevaMatriz);
        var matrizNueva = await GetMatrizAsync(rolId);

        await _auditoria.RegistrarAsync(
            ModuloSistema.Permisos,
            AccionPermiso.Administrar,
            $"Matriz de permisos actualizada para el rol '{rol.Nombre}'.",
            rolId,
            entidad: "RolPermiso",
            valoresAnteriores: matrizAnterior.Where(x => x.Permitido).ToList(),
            valoresNuevos: matrizNueva.Where(x => x.Permitido).ToList());

        return matrizNueva;
    }

    public async Task PrecargarMatrizPorDefectoAsync(int rolId, bool esAdministrador)
    {
        if (esAdministrador) return;
        if (await _repository.TieneMatrizDefinidaAsync(rolId)) return;

        var filas = new List<RolPermiso>();
        foreach (var d in CatalogoPermisosBase.DefaultVendedor)
        {
            var permiso = await _permisoRepository.GetByModuloAccionAsync(d.Modulo, d.Accion);
            if (permiso is null) continue;

            filas.Add(new RolPermiso
            {
                Rol = RolUsuario.Vendedor,
                RolId = rolId,
                PermisoId = permiso.Id,
                Modulo = d.Modulo,
                Accion = d.Accion,
                Permitido = true
            });
        }

        await _repository.AgregarSiFaltaAsync(filas);
    }

    public async Task<MisPermisosDto> GetMisPermisosAsync()
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        if (alcance is null)
        {
            return new MisPermisosDto
            {
                Rol = string.Empty,
                EsAdministrador = false,
                Permisos = new List<string>()
            };
        }

        if (alcance.EsAdministrador)
        {
            var todos = CatalogoPermisosBase.Definicion
                .SelectMany(m => m.Acciones.Select(a => $"{m.Modulo}:{a}"))
                .ToList();

            return new MisPermisosDto
            {
                Rol = alcance.RolNombre,
                EsAdministrador = true,
                Permisos = todos
            };
        }

        var filas = await _repository.GetByRolIdAsync(alcance.RolId);
        var permisos = filas
            .Where(p => p.Permitido)
            .Select(p => $"{p.Modulo}:{p.Accion}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new MisPermisosDto
        {
            Rol = alcance.RolNombre,
            EsAdministrador = false,
            Permisos = permisos
        };
    }

    public async Task<bool> TienePermisoAsync(ModuloSistema modulo, AccionPermiso accion)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        if (alcance is null) return false;
        if (alcance.EsAdministrador) return true;

        return await _repository.TienePermisoPorRolIdAsync(alcance.RolId, modulo, accion);
    }

    public async Task VerificarPermisoAsync(ModuloSistema modulo, AccionPermiso accion)
    {
        if (!await TienePermisoAsync(modulo, accion))
            throw new ForbiddenAccessException($"No tienes permiso para '{accion}' en el módulo '{modulo}'.");
    }
}
