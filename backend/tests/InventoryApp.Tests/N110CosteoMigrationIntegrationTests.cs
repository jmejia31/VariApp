using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryApp.Tests;

[Trait("Category", "Integration")]
public sealed class N110CosteoMigrationIntegrationTests
{
    private static DbContextOptions<AppDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                $"Server=localhost;Port=3306;Database={dbName};User=root;Password=root;",
                new MySqlServerVersion(new Version(8, 4, 3)))
            .Options;

    [Fact]
    public async Task MigrateAsync_BaseVacia_CreaEmpresaActivaYPoliticaPromedioPonderado()
    {
        var dbName = $"test_n110_costeo_bootstrap_{Guid.NewGuid():N}";
        var options = CreateOptions(dbName);

        await using var context = new AppDbContext(options);
        try
        {
            await context.Database.MigrateAsync();

            var empresas = await context.EmpresaConfiguraciones.AsNoTracking().ToListAsync();
            var politicas = await context.Set<PoliticaCosteoInventario>().AsNoTracking().ToListAsync();

            var empresa = Assert.Single(empresas);
            Assert.True(empresa.Activa);
            Assert.Equal("VariStorehn", empresa.NombreComercial);
            Assert.Equal("HNL", empresa.Moneda);
            Assert.Equal("America/Tegucigalpa", empresa.ZonaHoraria);

            var politica = Assert.Single(politicas);
            Assert.Equal(empresa.Id, politica.EmpresaConfiguracionId);
            Assert.Equal(MetodoCosteoInventario.PromedioPonderado, politica.Metodo);
            Assert.True(politica.EstaVigente);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
