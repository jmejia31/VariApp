using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryApp.Tests;

public class PermisoSeedServiceTests
{
    [Fact]
    public async Task SeedDefaults_CreaPermisosParaRolesBasicos_YNoDuplicaExistentes()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        context.RolPermisos.Add(new RolPermiso
        {
            Rol = RolUsuario.Vendedor,
            Modulo = ModuloSistema.Productos,
            Accion = AccionPermiso.Ver,
            Permitido = true
        });

        await context.SaveChangesAsync();

        var seedService = new SeedPermisoService(context);
        await seedService.SeedDefaultsAsync();

        var permisos = await context.RolPermisos
            .Where(p => p.Rol == RolUsuario.Vendedor || p.Rol == RolUsuario.Administrador)
            .OrderBy(p => p.Rol)
            .ThenBy(p => p.Modulo)
            .ThenBy(p => p.Accion)
            .ToListAsync();

        Assert.Contains(permisos, p => p.Rol == RolUsuario.Vendedor && p.Modulo == ModuloSistema.Productos && p.Accion == AccionPermiso.Ver && p.Permitido);
        Assert.Contains(permisos, p => p.Rol == RolUsuario.Vendedor && p.Modulo == ModuloSistema.Compras && p.Accion == AccionPermiso.Crear && p.Permitido);
        Assert.Contains(permisos, p => p.Rol == RolUsuario.Administrador && p.Modulo == ModuloSistema.Productos && p.Accion == AccionPermiso.Crear && p.Permitido);

        var vendedorProductosVer = permisos.Count(p => p.Rol == RolUsuario.Vendedor && p.Modulo == ModuloSistema.Productos && p.Accion == AccionPermiso.Ver);
        Assert.Equal(1, vendedorProductosVer);

        Assert.Contains(await context.Permisos.ToListAsync(), p =>
            p.Modulo == ModuloSistema.Configuracion &&
            p.Accion == AccionPermiso.Editar &&
            p.Activo &&
            p.EsSistema);
    }

    [Fact]
    public async Task SeedDefaults_NoReactivaPermisosDesactivadosManualmente()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        context.RolPermisos.Add(new RolPermiso
        {
            Rol = RolUsuario.Vendedor,
            Modulo = ModuloSistema.Ventas,
            Accion = AccionPermiso.Crear,
            Permitido = false
        });
        await context.SaveChangesAsync();

        var seedService = new SeedPermisoService(context);
        await seedService.SeedDefaultsAsync();

        var permiso = await context.RolPermisos.SingleAsync(p =>
            p.Rol == RolUsuario.Vendedor &&
            p.Modulo == ModuloSistema.Ventas &&
            p.Accion == AccionPermiso.Crear);

        Assert.False(permiso.Permitido);
    }
}
