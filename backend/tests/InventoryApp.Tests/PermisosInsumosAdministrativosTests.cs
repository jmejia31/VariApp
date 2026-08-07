using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryApp.Tests;

public class PermisosInsumosAdministrativosTests
{
    private static AppDbContext CrearContexto() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task SeedDefaultsAsync_Agrega_Permisos_Nuevos_Al_Administrador_Sin_Tocar_Vendedor()
    {
        await using var context = CrearContexto();
        var service = new SeedPermisoService(context);
        await service.SeedDefaultsAsync();

        var administrador = await context.Roles.IgnoreQueryFilters()
            .SingleAsync(r => r.EsAdministrador);
        var vendedor = await context.Roles.IgnoreQueryFilters()
            .SingleAsync(r => r.NombreNormalizado == "VENDEDOR");

        var adminInsumos = await context.RolPermisos
            .Where(rp => rp.RolId == administrador.Id &&
                         rp.Modulo == ModuloSistema.InsumosAdministrativos)
            .ToListAsync();
        Assert.NotEmpty(adminInsumos);
        Assert.All(adminInsumos, rp => Assert.True(rp.Permitido));
        Assert.Contains(adminInsumos, rp => rp.Accion == AccionPermiso.RegistrarConsumo);
        Assert.Contains(adminInsumos, rp => rp.Accion == AccionPermiso.AjustarStock);

        Assert.False(await context.RolPermisos.AnyAsync(rp =>
            rp.RolId == vendedor.Id && rp.Modulo == ModuloSistema.InsumosAdministrativos));
    }

    [Fact]
    public async Task SeedDefaultsAsync_No_Sobrescribe_Un_Permiso_Nuevo_Denegado_Expresamente()
    {
        await using var context = CrearContexto();
        var service = new SeedPermisoService(context);
        await service.SeedDefaultsAsync();

        var administrador = await context.Roles.IgnoreQueryFilters()
            .SingleAsync(r => r.EsAdministrador);
        var exonerar = await context.RolPermisos.SingleAsync(rp =>
            rp.RolId == administrador.Id &&
            rp.Modulo == ModuloSistema.Ventas &&
            rp.Accion == AccionPermiso.ExonerarEnvio);
        exonerar.Permitido = false;
        await context.SaveChangesAsync();

        await service.SeedDefaultsAsync();

        var filas = await context.RolPermisos.Where(rp =>
            rp.RolId == administrador.Id &&
            rp.Modulo == ModuloSistema.Ventas &&
            rp.Accion == AccionPermiso.ExonerarEnvio).ToListAsync();
        var fila = Assert.Single(filas);
        Assert.False(fila.Permitido);
    }
}
