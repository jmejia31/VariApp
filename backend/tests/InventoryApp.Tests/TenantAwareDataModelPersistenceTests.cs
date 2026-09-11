using System.Reflection;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Migrations;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace InventoryApp.Tests;

public sealed class TenantAwareDataModelPersistenceTests
{
    [Fact]
    public void Sucursal_EmpresaId_EsRequerido_Y_FkRestrict()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n62c-{Guid.NewGuid()}")
            .Options;

        using var db = new AppDbContext(options);
        var sucursal = db.Model.FindEntityType(typeof(Sucursal));
        Assert.NotNull(sucursal);

        var empresaId = sucursal!.FindProperty(nameof(Sucursal.EmpresaId));
        Assert.NotNull(empresaId);
        Assert.False(empresaId!.IsNullable);

        var fk = Assert.Single(sucursal.GetForeignKeys(), candidate =>
            candidate.PrincipalEntityType.ClrType == typeof(Empresa) &&
            candidate.Properties.Count == 1 &&
            candidate.Properties[0].Name == nameof(Sucursal.EmpresaId));

        Assert.True(fk.IsRequired);
        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
    }

    [Fact]
    public void Ownership_Descendente_NoDuplica_EmpresaId()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n62c-{Guid.NewGuid()}")
            .Options;

        using var db = new AppDbContext(options);
        var almacen = db.Model.FindEntityType(typeof(Almacen));
        var ubicacion = db.Model.FindEntityType(typeof(UbicacionAlmacen));

        Assert.NotNull(almacen);
        Assert.NotNull(ubicacion);
        Assert.Null(almacen!.FindProperty("EmpresaId"));
        Assert.Null(ubicacion!.FindProperty("EmpresaId"));
    }

    [Fact]
    public void Migracion_EsAtomica_Y_FallaCerrado_SinBackfillArbitrario()
    {
        var migration = new N62CTenantAwareEmpresaId();
        var up = typeof(N62CTenantAwareEmpresaId)
            .GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(up);

        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
        up!.Invoke(migration, new object[] { builder });

        var operation = Assert.Single(builder.Operations.OfType<SqlOperation>());
        Assert.Contains("ALTER TABLE `Sucursales`", operation.Sql, StringComparison.Ordinal);
        Assert.Contains("MODIFY COLUMN `EmpresaId` int NOT NULL", operation.Sql, StringComparison.Ordinal);
        Assert.Contains("FK_Sucursales_Empresas_EmpresaId", operation.Sql, StringComparison.Ordinal);
        Assert.Contains("REFERENCES `Empresas` (`Id`)", operation.Sql, StringComparison.Ordinal);
        Assert.Contains("ON DELETE RESTRICT", operation.Sql, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE `Sucursales`", operation.Sql, StringComparison.OrdinalIgnoreCase);
    }
}
