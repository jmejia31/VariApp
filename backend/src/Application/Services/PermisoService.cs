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
        _usuarioScope = usuarioScope;
        _ = currentUser;
    }

    public async Task<List<PermisoMatrizItemDto>> GetMatrizAsync(int rolId)
    {
        var rol = await _rolRepository.GetByIdAsync(rolId)
            ?? throw new BusinessRuleException("El rol seleccionado no existe.");

        var existentes = await _repository.GetByRolIdAsync(rolId);
        var catalogo = (await _permisoRepository.GetAllAsync())
            .Where(p => p.Activo && !p.Eliminado)
            .OrderBy(p => p.Modulo)
            .ThenBy(p => p.Accion)
            .ToList();

        return catalogo.Select(permiso => new PermisoMatrizItemDto
        {
            Rol = rol.Nombre,
            Modulo = permiso.Modulo.ToString(),
            Accion = permiso.Accion.ToString(),
            Permitido = existentes.Any(p => p.PermisoId == permiso.Id)
        }).ToList();
    }

    public async Task<List<PermisoMatrizItemDto>> UpdateMatrizAsync(int rolId, UpdatePermisoMatrizDto dto)
    {
        var rol = await _rolRepository.GetByIdAsync(rolId)
            ?? throw new BusinessRuleException("El rol seleccionado no existe.");
        if (!rol.Activo)
            throw new BusinessRuleException("No se pueden asignar permisos a un rol inactivo.");

        var catalogoActivo = (await _permisoRepository.GetAllAsync())
            .Where(p => p.Activo && !p.Eliminado)
            .ToList();
        var nuevaMatriz = new List<RolPermiso>();
        var permisoIds = new HashSet<int>();

        foreach (var item in dto.Permisos.Where(p => p.Permitido))
        {
            if (!Enum.TryParse<ModuloSistema>(item.Modulo, true, out var modulo) ||
                !Enum.TryParse<AccionPermiso>(item.Accion, true, out var accion))
                throw new BusinessRuleException($"Combinación inválida '{item.Modulo}.{item.Accion}'.");

            // La validez y vigencia de la combinación proviene del catálogo persistido,
            // no de una matriz hardcodeada en la capa de autorización.
            var permiso = await _permisoRepository.GetByModuloAccionAsync(modulo, accion)
                ?? throw new BusinessRuleException($"El permiso '{modulo}:{accion}' no existe, está inactivo o fue eliminado.");

            if (!permisoIds.Add(permiso.Id))
                throw new BusinessRuleException($"El permiso '{permiso.Codigo}' viene duplicado en la solicitud.");

            nuevaMatriz.Add(new RolPermiso
            {
                RolId = rolId,
                PermisoId = permiso.Id
            });
        }

        if (rol.EsAdministrador)
        {
            var permisosActivos = catalogoActivo.Select(p => p.Id).ToHashSet();
            if (!permisoIds.SetEquals(permisosActivos))
            {
                throw new BusinessRuleException(
                    "Los roles administradores deben conservar grants explícitos para todo el catálogo activo.");
            }
        }

        // La matriz solo se reemplaza después de validar por completo la invariancia
        // administrativa. Un rechazo jamás puede dejar grants parcialmente mutados.
        var matrizAnterior = await GetMatrizAsync(rolId);
        await _repository.ReemplazarMatrizPorRolIdAsync(rolId, nuevaMatriz);
        var matrizNueva = await GetMatrizAsync(rolId);

        await _auditoria.RegistrarAsync(
            ModuloSistema.Permisos,
            AccionPermiso.Administrar,
            $"Matriz relacional de permisos actualizada para el rol '{rol.Nombre}'.",
            rolId,
            entidad: "RolPermiso",
            valoresAnteriores: matrizAnterior.Where(x => x.Permitido).ToList(),
            valoresNuevos: matrizNueva.Where(x => x.Permitido).ToList());

        return matrizNueva;
    }

    public async Task PrecargarMatrizPorDefectoAsync(int rolId, bool esAdministrador)
    {
        if (await _repository.TieneMatrizDefinidaAsync(rolId)) return;

        var filas = new List<RolPermiso>();
        if (esAdministrador)
        {
            foreach (var permiso in (await _permisoRepository.GetAllAsync()).Where(p => p.Activo && !p.Eliminado))
            {
                filas.Add(new RolPermiso { RolId = rolId, PermisoId = permiso.Id });
            }
        }
        else
        {
            foreach (var d in CatalogoPermisosBase.DefaultVendedor)
            {
                var permiso = await _permisoRepository.GetByModuloAccionAsync(d.Modulo, d.Accion);
                if (permiso is not null)
                    filas.Add(new RolPermiso { RolId = rolId, PermisoId = permiso.Id });
            }
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

        var filas = await _repository.GetByRolIdAsync(alcance.RolId);
        var permisos = filas
            .Where(p => p.Permiso is { Activo: true, Eliminado: false })
            .Select(p => $"{p.Permiso.Modulo}:{p.Permiso.Accion}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new MisPermisosDto
        {
            Rol = alcance.RolNombre,
            EsAdministrador = alcance.EsAdministrador,
            Permisos = permisos
        };
    }

    public async Task<bool> TienePermisoAsync(ModuloSistema modulo, AccionPermiso accion)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        if (alcance is null) return false;

        // No existe bypass para EsAdministrador: un administrador tiene acceso
        // porque su rol posee grants explícitos en RolPermiso.
        return await _repository.TienePermisoPorRolIdAsync(alcance.RolId, modulo, accion);
    }

    public async Task VerificarPermisoAsync(ModuloSistema modulo, AccionPermiso accion)
    {
        if (!await TienePermisoAsync(modulo, accion))
            throw new ForbiddenAccessException($"No tienes permiso para '{accion}' en el módulo '{modulo}'.");
    }
}
