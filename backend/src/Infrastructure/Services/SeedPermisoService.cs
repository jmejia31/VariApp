using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Application.Common;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Services;

public class SeedPermisoService
{
    private readonly AppDbContext _context;

    public SeedPermisoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedDefaultsAsync()
    {
        await SeedCatalogoPermisosAsync();

        var defaults = new Dictionary<RolUsuario, List<(ModuloSistema Modulo, AccionPermiso Accion, bool Permitido)>>()
        {
            [RolUsuario.Vendedor] = new()
            {
                (ModuloSistema.Dashboard, AccionPermiso.Ver, true),
                (ModuloSistema.Productos, AccionPermiso.Ver, true),
                (ModuloSistema.Categorias, AccionPermiso.Ver, true),
                (ModuloSistema.Compras, AccionPermiso.Ver, true),
                (ModuloSistema.Compras, AccionPermiso.Crear, true),
                (ModuloSistema.Ventas, AccionPermiso.Ver, true),
                (ModuloSistema.Ventas, AccionPermiso.Crear, true),
                (ModuloSistema.Ventas, AccionPermiso.Editar, true),
                (ModuloSistema.Ventas, AccionPermiso.Confirmar, true),
                (ModuloSistema.Facturacion, AccionPermiso.Ver, true),
                (ModuloSistema.Finanzas, AccionPermiso.Ver, true),
                (ModuloSistema.Inventario, AccionPermiso.Ver, true),
                (ModuloSistema.Proveedores, AccionPermiso.Ver, true),
                (ModuloSistema.Clientes, AccionPermiso.Ver, true),
                (ModuloSistema.Clientes, AccionPermiso.Crear, true),
                (ModuloSistema.Clientes, AccionPermiso.Editar, true),
                (ModuloSistema.Auditoria, AccionPermiso.Ver, true)
            },
            [RolUsuario.Administrador] = new()
            {
                (ModuloSistema.Dashboard, AccionPermiso.Ver, true),
                (ModuloSistema.Productos, AccionPermiso.Ver, true),
                (ModuloSistema.Productos, AccionPermiso.Crear, true),
                (ModuloSistema.Productos, AccionPermiso.Editar, true),
                (ModuloSistema.Productos, AccionPermiso.Eliminar, true),
                (ModuloSistema.Categorias, AccionPermiso.Ver, true),
                (ModuloSistema.Categorias, AccionPermiso.Crear, true),
                (ModuloSistema.Categorias, AccionPermiso.Editar, true),
                (ModuloSistema.Categorias, AccionPermiso.Eliminar, true),
                (ModuloSistema.Compras, AccionPermiso.Ver, true),
                (ModuloSistema.Compras, AccionPermiso.Crear, true),
                (ModuloSistema.Compras, AccionPermiso.Editar, true),
                (ModuloSistema.Compras, AccionPermiso.Confirmar, true),
                (ModuloSistema.Compras, AccionPermiso.Anular, true),
                (ModuloSistema.Ventas, AccionPermiso.Ver, true),
                (ModuloSistema.Ventas, AccionPermiso.Crear, true),
                (ModuloSistema.Ventas, AccionPermiso.Editar, true),
                (ModuloSistema.Ventas, AccionPermiso.Confirmar, true),
                (ModuloSistema.Ventas, AccionPermiso.Anular, true),
                (ModuloSistema.Facturacion, AccionPermiso.Ver, true),
                (ModuloSistema.Finanzas, AccionPermiso.Ver, true),
                (ModuloSistema.Finanzas, AccionPermiso.Crear, true),
                (ModuloSistema.Finanzas, AccionPermiso.Anular, true),
                (ModuloSistema.Inventario, AccionPermiso.Ver, true),
                (ModuloSistema.Proveedores, AccionPermiso.Ver, true),
                (ModuloSistema.Proveedores, AccionPermiso.Crear, true),
                (ModuloSistema.Proveedores, AccionPermiso.Editar, true),
                (ModuloSistema.Proveedores, AccionPermiso.Eliminar, true),
                (ModuloSistema.Clientes, AccionPermiso.Ver, true),
                (ModuloSistema.Clientes, AccionPermiso.Crear, true),
                (ModuloSistema.Clientes, AccionPermiso.Editar, true),
                (ModuloSistema.Clientes, AccionPermiso.Eliminar, true),
                (ModuloSistema.Auditoria, AccionPermiso.Ver, true)
            }
        };

        foreach (var (rol, entries) in defaults)
        {
            foreach (var (modulo, accion, permitido) in entries)
            {
                var existing = await _context.RolPermisos
                    .FirstOrDefaultAsync(p => p.Rol == rol && p.Modulo == modulo && p.Accion == accion);

                if (existing is null)
                {
                    _context.RolPermisos.Add(new RolPermiso
                    {
                        Rol = rol,
                        Modulo = modulo,
                        Accion = accion,
                        Permitido = permitido
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedCatalogoPermisosAsync()
    {
        foreach (var (modulo, acciones) in CatalogoPermisosBase.Definicion)
        {
            foreach (var accion in acciones)
            {
                var exists = await _context.Permisos
                    .AnyAsync(p => p.Modulo == modulo && p.Accion == accion);

                if (exists) continue;

                _context.Permisos.Add(new Permiso
                {
                    Codigo = $"{modulo}.{accion}".ToUpperInvariant(),
                    Nombre = $"{modulo} - {accion}",
                    Descripcion = $"Permite {accion} en {modulo}.",
                    Modulo = modulo,
                    Accion = accion,
                    EsSistema = true,
                    Activo = true,
                    Eliminado = false
                });
            }
        }
    }
}
