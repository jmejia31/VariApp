using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class PermisoService : IPermisoService
{
    private static readonly (ModuloSistema Modulo, AccionPermiso[] Acciones)[] ModulosConfigurables =
    {
        (ModuloSistema.Dashboard, new[] { AccionPermiso.Ver }),
        (ModuloSistema.Productos, new[] { AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar, AccionPermiso.Eliminar }),
        (ModuloSistema.Categorias, new[] { AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar, AccionPermiso.Eliminar }),
        (ModuloSistema.Compras, new[] { AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar, AccionPermiso.Confirmar, AccionPermiso.Anular }),
        (ModuloSistema.Ventas, new[] { AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar, AccionPermiso.Confirmar, AccionPermiso.Anular }),
        (ModuloSistema.Facturacion, new[] { AccionPermiso.Ver }),
        (ModuloSistema.Finanzas, new[] { AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Anular }),
        (ModuloSistema.Inventario, new[] { AccionPermiso.Ver }),
        (ModuloSistema.Proveedores, new[] { AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar, AccionPermiso.Eliminar }),
        (ModuloSistema.Clientes, new[] { AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar, AccionPermiso.Eliminar }),
        (ModuloSistema.Auditoria, new[] { AccionPermiso.Ver }),
    };

    private readonly IRolPermisoRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public PermisoService(IRolPermisoRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<List<PermisoMatrizItemDto>> GetMatrizAsync()
    {
        var existentes = await _repository.GetByRolAsync(RolUsuario.Vendedor);
        var resultado = new List<PermisoMatrizItemDto>();

        foreach (var (modulo, acciones) in ModulosConfigurables)
        {
            foreach (var accion in acciones)
            {
                var fila = existentes.FirstOrDefault(p => p.Modulo == modulo && p.Accion == accion);
                resultado.Add(new PermisoMatrizItemDto
                {
                    Rol = RolUsuario.Vendedor.ToString(),
                    Modulo = modulo.ToString(),
                    Accion = accion.ToString(),
                    Permitido = fila?.Permitido ?? false
                });
            }
        }

        return resultado;
    }

    public async Task<List<PermisoMatrizItemDto>> UpdateMatrizAsync(UpdatePermisoMatrizDto dto)
    {
        var nuevaMatriz = dto.Permisos
            .Where(p => p.Permitido)
            .Select(p => new RolPermiso
            {
                Rol = RolUsuario.Vendedor,
                Modulo = Enum.Parse<ModuloSistema>(p.Modulo),
                Accion = Enum.Parse<AccionPermiso>(p.Accion),
                Permitido = true
            })
            .ToList();

        await _repository.ReemplazarMatrizAsync(nuevaMatriz);
        return await GetMatrizAsync();
    }

    public async Task<MisPermisosDto> GetMisPermisosAsync()
    {
        var rol = _currentUser.Rol ?? RolUsuario.Vendedor;

        if (rol == RolUsuario.Administrador)
        {
            var todos = ModulosConfigurables
                .SelectMany(m => m.Acciones.Select(a => $"{m.Modulo}:{a}"))
                .ToList();

            return new MisPermisosDto { Rol = rol.ToString(), EsAdministrador = true, Permisos = todos };
        }

        var filas = await _repository.GetByRolAsync(rol);
        var permisos = filas.Where(p => p.Permitido).Select(p => $"{p.Modulo}:{p.Accion}").ToList();

        return new MisPermisosDto { Rol = rol.ToString(), EsAdministrador = false, Permisos = permisos };
    }

    public async Task<bool> TienePermisoAsync(ModuloSistema modulo, AccionPermiso accion)
    {
        var rol = _currentUser.Rol ?? RolUsuario.Vendedor;
        if (rol == RolUsuario.Administrador) return true;

        var permisos = await _repository.GetByRolAsync(rol);
        if (permisos.Count == 0) return false;

        return permisos.Any(p => p.Modulo == modulo && p.Accion == accion && p.Permitido);
    }

    public async Task VerificarPermisoAsync(ModuloSistema modulo, AccionPermiso accion)
    {
        if (!await TienePermisoAsync(modulo, accion))
            throw new ForbiddenAccessException($"No tienes permiso para '{accion}' en el módulo '{modulo}'.");
    }
}
