using System.Reflection;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N310CreditoClienteMigrationSnapshotTests
{
    [Fact]
    public void MigracionN310C_MaterializaContratoYRollbackFailClosed()
    {
        var migration = new N3_10_CreditoClientePersistencia();
        var upBuilder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
        InvokeMigrationMethod(migration, "Up", upBuilder);

        var upSql = upBuilder.Operations
            .OfType<SqlOperation>()
            .Select(x => x.Sql)
            .ToArray();

        Assert.Contains(upSql, sql => sql.Contains("CREATE TABLE `CreditosCliente`", StringComparison.Ordinal));
        Assert.Contains(upSql, sql => sql.Contains("FK_CreditosCliente_Clientes_ClienteId", StringComparison.Ordinal)
                                      && sql.Contains("ON DELETE RESTRICT", StringComparison.Ordinal));
        Assert.Contains(upSql, sql => sql.Contains("IX_CreditosCliente_ClienteId", StringComparison.Ordinal));
        Assert.Contains(upSql, sql => sql.Contains("__N310CPostGuard", StringComparison.Ordinal));

        var downBuilder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
        InvokeMigrationMethod(migration, "Down", downBuilder);
        var downOperations = downBuilder.Operations.ToList();

        var guardIndex = downOperations.FindIndex(operation =>
            operation is SqlOperation sql && sql.Sql.Contains("__N310CDownGuard", StringComparison.Ordinal));
        var dropIndex = downOperations.FindIndex(operation =>
            operation is DropTableOperation drop && drop.Name == "CreditosCliente");

        Assert.True(guardIndex >= 0, "La migración debe ejecutar el DownGuard fail-closed.");
        Assert.True(dropIndex > guardIndex, "El DownGuard debe ejecutarse antes de eliminar CreditosCliente.");
    }

    [Fact]
    public void AppDbContext_ExponeDbSetCreditoClienteConTipoCorrecto()
    {
        var property = typeof(AppDbContext).GetProperty(nameof(AppDbContext.CreditosCliente));

        Assert.NotNull(property);
        Assert.Equal(typeof(DbSet<CreditoCliente>), property!.PropertyType);
    }

    [Fact]
    public void SnapshotPart28_EstaCableadoEnSnapshotRaizYConservaContratoCreditoCliente()
    {
        var infrastructureAssembly = typeof(AppDbContext).Assembly;
        var snapshotType = infrastructureAssembly.GetType(
            "InventoryApp.Infrastructure.Migrations.AppDbContextModelSnapshot",
            throwOnError: false);

        Assert.NotNull(snapshotType);

        var snapshot = Activator.CreateInstance(snapshotType!, nonPublic: true) as ModelSnapshot;
        Assert.NotNull(snapshot);

        var entity = snapshot!.Model.FindEntityType(typeof(CreditoCliente));
        Assert.NotNull(entity);
        Assert.Equal("CreditosCliente", entity!.GetTableName());
        Assert.Contains(entity.GetIndexes(), index => index.GetDatabaseName() == "IX_CreditosCliente_ClienteId");
        Assert.Contains(entity.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(Cliente) && fk.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Equal("decimal(18,4)", entity.FindProperty(nameof(CreditoCliente.LimiteCredito))!.GetColumnType());
        Assert.Equal(3, entity.FindProperty(nameof(CreditoCliente.Moneda))!.GetMaxLength());
    }

    private static void InvokeMigrationMethod(Migration migration, string methodName, MigrationBuilder builder)
    {
        var method = migration.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(migration, new object[] { builder });
    }
}