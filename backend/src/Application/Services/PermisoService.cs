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
    private readonly ICurrentUserService _currentUser;

    public PermisoService(IRolPermisoRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
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
        // Validamos que cada combinación enviada sea una de las válidas del catálogo
        // (nunca confiamos ciegamente en lo que llega del frontend).
        var nuevaMatriz = new List<RolPermiso>();
        foreach (var item in dto.Permisos.Where(p => p.Permitido))
        {
            if (!Enum.TryParse<ModuloSistema>(item.Modulo, out var modulo) ||
                !Enum.TryParse<AccionPermiso>(item.Accion, out var accion))
                throw new BusinessRuleException($"Combinación inválida '{item.Modulo}.{item.Accion}'.");

            var valido = CatalogoPermisosBase.Definicion.Any(d => d.Modulo == modulo && d.Acciones.Contains(accion));
            if (!valido)
                throw new BusinessRuleException($"La acción '{accion}' no aplica al módulo '{modulo}'.");

            nuevaMatriz.Add(new RolPermiso { RolId = rolId, Modulo = modulo, Accion = accion, Permitido = true });
        }

        await _repository.ReemplazarMatrizPorRolIdAsync(rolId, nuevaMatriz);
        return await GetMatrizAsync(rolId);
    }

    public async Task PrecargarMatrizPorDefectoAsync(int rolId, bool esAdministrador)
    {
        // Idempotente: si el rol ya tiene una matriz definida (aunque sea parcial),
        // no se toca nada. El administrador dinámico no depende de estas filas
        // (tiene acceso total por Rol.EsAdministrador), así que no se siembra nada
        // para él salvo que se quiera dejar explícito en la UI.
        if (esAdministrador) return;
        if (await _repository.TieneMatrizDefinidaAsync(rolId)) return;

        var filas = CatalogoPermisosBase.DefaultVendedor
            .Select(d => new RolPermiso { RolId = rolId, Modulo = d.Modulo, Accion = d.Accion, Permitido = true })
            .ToList();

        await _repository.AgregarSiFaltaAsync(filas);
    }

    public async Task<MisPermisosDto> GetMisPermisosAsync()
    {
        if (_currentUser.EsAdministrador)
        {
            var todos = CatalogoPermisosBase.Definicion
                .SelectMany(m => m.Acciones.Select(a => $"{m.Modulo}:{a}"))
                .ToList();

            return new MisPermisosDto { Rol = _currentUser.Rol?.ToString() ?? "Administrador", EsAdministrador = true, Permisos = todos };
        }

        var rolId = _currentUser.RolId;
        List<string> permisos;

        if (rolId.HasValue)
        {
            var filas = await _repository.GetByRolIdAsync(rolId.Value);
            permisos = filas.Where(p => p.Permitido).Select(p => $"{p.Modulo}:{p.Accion}").ToList();
        }
        else
        {
            // Fallback legado: usuario aún no migrado al catálogo dinámico de roles.
            var rolLegado = _currentUser.Rol ?? RolUsuario.Vendedor;
            var filas = await _repository.GetByRolAsync(rolLegado);
            permisos = filas.Where(p => p.Permitido).Select(p => $"{p.Modulo}:{p.Accion}").ToList();
        }

        return new MisPermisosDto { Rol = _currentUser.Rol?.ToString() ?? string.Empty, EsAdministrador = false, Permisos = permisos };
    }

    public async Task<bool> TienePermisoAsync(ModuloSistema modulo, AccionPermiso accion)
    {
        if (_currentUser.EsAdministrador) return true;

        if (_currentUser.RolId.HasValue)
            return await _repository.TienePermisoPorRolIdAsync(_currentUser.RolId.Value, modulo, accion);

        // Fallback legado (usuarios/JWTs sin migrar todavía).
        var rolLegado = _currentUser.Rol ?? RolUsuario.Vendedor;
        return await _repository.TienePermisoAsync(rolLegado, modulo, accion);
    }

    public async Task VerificarPermisoAsync(ModuloSistema modulo, AccionPermiso accion)
    {
        if (!await TienePermisoAsync(modulo, accion))
            throw new ForbiddenAccessException($"No tienes permiso para '{accion}' en el módulo '{modulo}'.");
    }
}
