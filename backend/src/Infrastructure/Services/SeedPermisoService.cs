using InventoryApp.Application.Common;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Services;

public class SeedPermisoService
{
    private const string AdministradorNormalizado = "ADMINISTRADOR";
    private const string VendedorNormalizado = "VENDEDOR";

    private readonly AppDbContext _context;

    public SeedPermisoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedDefaultsAsync()
    {
        await SeedCatalogoPermisosAsync();
        await _context.SaveChangesAsync();

        var roles = await SeedRolesSistemaAsync();
        await VincularUsuariosLegacyAsync(roles.Administrador, roles.Vendedor);
        await VincularPermisosLegacyAsync(roles.Administrador, roles.Vendedor);

        // Los valores predeterminados solo se insertan cuando el rol fue creado
        // en este mismo arranque. Una matriz existente, incluso vacía por decisión
        // administrativa, nunca se reconstruye ni sobrescribe automáticamente.
        if (roles.AdministradorCreado)
            await SeedAdministradorInicialAsync(roles.Administrador);

        if (roles.VendedorCreado)
            await SeedVendedorInicialAsync(roles.Vendedor);

        await _context.SaveChangesAsync();
    }

    private async Task<(Rol Administrador, Rol Vendedor, bool AdministradorCreado, bool VendedorCreado)>
        SeedRolesSistemaAsync()
    {
        var existentes = await _context.Roles
            .IgnoreQueryFilters()
            .ToListAsync();

        var administrador = existentes.FirstOrDefault(r =>
            r.NombreNormalizado == AdministradorNormalizado ||
            string.Equals(r.Nombre, "Administrador", StringComparison.OrdinalIgnoreCase));
        var administradorCreado = administrador is null;
        if (administrador is null)
        {
            administrador = new Rol
            {
                Nombre = "Administrador",
                NombreNormalizado = AdministradorNormalizado,
                Descripcion = "Rol de sistema con acceso administrativo total.",
                EsSistema = true,
                EsAdministrador = true,
                Activo = true,
                Eliminado = false,
                FechaCreacion = DateTime.UtcNow
            };
            _context.Roles.Add(administrador);
        }
        else if (string.IsNullOrWhiteSpace(administrador.NombreNormalizado))
        {
            administrador.NombreNormalizado = AdministradorNormalizado;
        }

        var vendedor = existentes.FirstOrDefault(r =>
            r.NombreNormalizado == VendedorNormalizado ||
            string.Equals(r.Nombre, "Vendedor", StringComparison.OrdinalIgnoreCase));
        var vendedorCreado = vendedor is null;
        if (vendedor is null)
        {
            vendedor = new Rol
            {
                Nombre = "Vendedor",
                NombreNormalizado = VendedorNormalizado,
                Descripcion = "Rol de sistema para operación comercial con permisos administrables.",
                EsSistema = true,
                EsAdministrador = false,
                Activo = true,
                Eliminado = false,
                FechaCreacion = DateTime.UtcNow
            };
            _context.Roles.Add(vendedor);
        }
        else if (string.IsNullOrWhiteSpace(vendedor.NombreNormalizado))
        {
            vendedor.NombreNormalizado = VendedorNormalizado;
        }

        await _context.SaveChangesAsync();
        return (administrador, vendedor, administradorCreado, vendedorCreado);
    }

    private async Task VincularUsuariosLegacyAsync(Rol administrador, Rol vendedor)
    {
        var usuariosSinRolDinamico = await _context.Usuarios
            .IgnoreQueryFilters()
            .Where(u => !u.RolId.HasValue)
            .ToListAsync();

        foreach (var usuario in usuariosSinRolDinamico)
        {
            usuario.RolId = usuario.Rol == RolUsuario.Administrador
                ? administrador.Id
                : vendedor.Id;
        }
    }

    private async Task VincularPermisosLegacyAsync(Rol administrador, Rol vendedor)
    {
        var catalogo = await _context.Permisos
            .IgnoreQueryFilters()
            .ToDictionaryAsync(p => (p.Modulo, p.Accion));

        var permisosLegacy = await _context.RolPermisos.ToListAsync();
        foreach (var permisoRol in permisosLegacy)
        {
            permisoRol.RolId ??= permisoRol.Rol == RolUsuario.Administrador
                ? administrador.Id
                : vendedor.Id;

            if (!permisoRol.PermisoId.HasValue &&
                catalogo.TryGetValue((permisoRol.Modulo, permisoRol.Accion), out var permisoCatalogo))
            {
                permisoRol.PermisoId = permisoCatalogo.Id;
            }
        }
    }

    private async Task SeedAdministradorInicialAsync(Rol administrador)
    {
        var catalogo = await _context.Permisos
            .IgnoreQueryFilters()
            .ToDictionaryAsync(p => (p.Modulo, p.Accion));

        foreach (var (modulo, acciones) in CatalogoPermisosBase.Definicion)
        {
            foreach (var accion in acciones)
            {
                if (!catalogo.TryGetValue((modulo, accion), out var permisoCatalogo))
                    continue;

                _context.RolPermisos.Add(new RolPermiso
                {
                    Rol = RolUsuario.Administrador,
                    RolId = administrador.Id,
                    PermisoId = permisoCatalogo.Id,
                    Modulo = modulo,
                    Accion = accion,
                    Permitido = true
                });
            }
        }
    }

    private async Task SeedVendedorInicialAsync(Rol vendedor)
    {
        if (CatalogoPermisosBase.DefaultVendedor.Length == 0)
            return;

        var catalogo = await _context.Permisos
            .IgnoreQueryFilters()
            .ToDictionaryAsync(p => (p.Modulo, p.Accion));

        foreach (var (modulo, accion) in CatalogoPermisosBase.DefaultVendedor)
        {
            if (!catalogo.TryGetValue((modulo, accion), out var permisoCatalogo))
                continue;

            _context.RolPermisos.Add(new RolPermiso
            {
                Rol = RolUsuario.Vendedor,
                RolId = vendedor.Id,
                PermisoId = permisoCatalogo.Id,
                Modulo = modulo,
                Accion = accion,
                Permitido = true
            });
        }
    }

    private async Task SeedCatalogoPermisosAsync()
    {
        foreach (var (modulo, acciones) in CatalogoPermisosBase.Definicion)
        {
            foreach (var accion in acciones)
            {
                var permiso = await _context.Permisos
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Modulo == modulo && p.Accion == accion);

                if (permiso is null)
                {
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
                    continue;
                }

                // El catálogo base define la identidad técnica de los permisos de
                // sistema. La matriz RolPermiso sí es administrable y no se toca aquí.
                permiso.Codigo = $"{modulo}.{accion}".ToUpperInvariant();
                permiso.Nombre = $"{modulo} - {accion}";
                permiso.Descripcion = $"Permite {accion} en {modulo}.";
                permiso.EsSistema = true;
                permiso.Activo = true;
                permiso.Eliminado = false;
                permiso.FechaEliminacion = null;
                permiso.EliminadoPorUsuarioId = null;
                permiso.FechaActualizacion = DateTime.UtcNow;
            }
        }
    }
}
