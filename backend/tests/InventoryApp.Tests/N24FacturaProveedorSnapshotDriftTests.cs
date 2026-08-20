using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N24FacturaProveedorSnapshotDriftTests
{
    [Fact]
    public void Snapshot_de_migraciones_debe_coincidir_con_modelo_de_diseno()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                "Server=127.0.0.1;Port=3306;Database=inventoryapp_snapshot_diagnostic;User=root;Password=root;SslMode=None;AllowPublicKeyRetrieval=True;",
                new MySqlServerVersion(new Version(8, 4, 0)))
            .Options;

        using var context = new AppDbContext(options);
        var designModel = context.GetService<IDesignTimeModel>().Model;
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();
        var snapshot = migrationsAssembly.ModelSnapshot;
        Assert.NotNull(snapshot);

        var differ = context.GetService<IMigrationsModelDiffer>();
        var operations = differ
            .GetDifferences(snapshot!.Model.GetRelationalModel(), designModel.GetRelationalModel())
            .Select(Describe)
            .ToArray();

        Assert.True(
            operations.Length == 0,
            "Drift EF detectado entre snapshot y modelo de diseño:\n" + string.Join("\n", operations));
    }

    private static string Describe(MigrationOperation operation) => operation switch
    {
        CreateTableOperation x => $"CreateTable {x.Name}",
        DropTableOperation x => $"DropTable {x.Name}",
        AddColumnOperation x => $"AddColumn {x.Table}.{x.Name} type={x.ColumnType} nullable={x.IsNullable}",
        AlterColumnOperation x => $"AlterColumn {x.Table}.{x.Name} type={x.ColumnType} nullable={x.IsNullable}",
        DropColumnOperation x => $"DropColumn {x.Table}.{x.Name}",
        CreateIndexOperation x => $"CreateIndex {x.Table}.{x.Name} ({string.Join(',', x.Columns)}) unique={x.IsUnique}",
        DropIndexOperation x => $"DropIndex {x.Table}.{x.Name}",
        AddForeignKeyOperation x => $"AddForeignKey {x.Table}.{x.Name} -> {x.PrincipalTable}",
        DropForeignKeyOperation x => $"DropForeignKey {x.Table}.{x.Name}",
        AddCheckConstraintOperation x => $"AddCheck {x.Table}.{x.Name}: {x.Sql}",
        DropCheckConstraintOperation x => $"DropCheck {x.Table}.{x.Name}",
        RenameColumnOperation x => $"RenameColumn {x.Table}.{x.Name} -> {x.NewName}",
        RenameIndexOperation x => $"RenameIndex {x.Table}.{x.Name} -> {x.NewName}",
        _ => $"{operation.GetType().Name}: {operation}"
    };
}
