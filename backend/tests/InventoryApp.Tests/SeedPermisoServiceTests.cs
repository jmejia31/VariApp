using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryApp.Tests;

public class SeedPermisoServiceTests
{
    private static AppDbContext CrearContexto(string nombre)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(nombre)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SeedDefaultsAsync_Crea_Roles_Sistema_Y_Vincula_Usuario_Legacy()
    {
        await using var context = CrearContexto(Guid.NewGuid().ToString());
        var usuario = new Usuario
        {
            NombreUsuario = "vendedor.legacy",
            NombreCompleto = "Vendedor Legacy",
            PasswordHash = "hash-no-usado",
            Rol = RolUsuario.Vendedor,
            RolId = null,
            Activo = true
        };
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();

        var service = new SeedPermisoService(context);
        await service.SeedDefaultsAsync();

        var roles = await context.Roles.IgnoreQueryFilters().ToListAsync();
        var administrador = Assert.Single(roles.Where(r => r.NombreNormalizado == "ADMINISTRADOR"));
        var vendedor = Assert.Single(roles.Where(r => r.NombreNormalizado == "VENDEDOR"));

        Assert.True(administrador.EsSistema);
        Assert.True(administrador.EsAdministrador);
        Assert.True(vendedor.EsSistema);
        Assert.False(vendedor.EsAdministrador);
        Assert.Equal(vendedor.Id, usuario.RolId);
        Assert.NotEmpty(await context.Permisos.IgnoreQueryFilters().ToListAsync());
        Assert.NotEmpty(await context.RolPermisos.Where(p => p.RolId == administrador.Id).ToListAsync());
    }

    [Fact]
    public async Task SeedDefaultsAsync_Segundo_Arranque_No_Sobrescribe_Matriz_Administrada()
    {
        await using var context = CrearContexto(Guid.NewGuid().ToString());
        var service = new SeedPermisoService(context);
        await service.SeedDefaultsAsync();

        var vendedor = await context.Roles
            .IgnoreQueryFilters()
            .SingleAsync(r => r.NombreNormalizado == "VENDEDOR");
        var permisoDashboard = await context.Permisos
            .IgnoreQueryFilters()
            .SingleAsync(p => p.Modulo == ModuloSistema.Dashboard && p.Accion == AccionPermiso.Ver);

        context.RolPermisos.Add(new RolPermiso
        {
            Rol = RolUsuario.Vendedor,
            RolId = vendedor.Id,
            PermisoId = permisoDashboard.Id,
            Modulo = ModuloSistema.Dashboard,
            Accion = AccionPermiso.Ver,
            Permitido = false
        });
        await context.SaveChangesAsync();

        await service.SeedDefaultsAsync();

        var filas = await context.RolPermisos
            .Where(p => p.RolId == vendedor.Id &&
                        p.Modulo == ModuloSistema.Dashboard &&
                        p.Accion == AccionPermiso.Ver)
            .ToListAsync();

        var fila = Assert.Single(filas);
        Assert.False(fila.Permitido);
    }

    [Fact]
    public async Task SeedDefaultsAsync_Vincula_Matriz_Legacy_Sin_Duplicarla()
    {
        await using var context = CrearContexto(Guid.NewGuid().ToString());
        context.RolPermisos.Add(new RolPermiso
        {
            Rol = RolUsuario.Administrador,
            RolId = null,
            PermisoId = null,
            Modulo = ModuloSistema.Dashboard,
            Accion = AccionPermiso.Ver,
            Permitido = true
        });
        await context.SaveChangesAsync();

        var service = new SeedPermisoService(context);
        await service.SeedDefaultsAsync();

        var administrador = await context.Roles
            .IgnoreQueryFilters()
            .SingleAsync(r => r.NombreNormalizado == "ADMINISTRADOR");
        var filasDashboard = await context.RolPermisos
            .Where(p => p.RolId == administrador.Id &&
                        p.Modulo == ModuloSistema.Dashboard &&
                        p.Accion == AccionPermiso.Ver)
            .ToListAsync();

        var fila = Assert.Single(filasDashboard);
        Assert.NotNull(fila.PermisoId);
        Assert.True(fila.Permitido);
    }
}
