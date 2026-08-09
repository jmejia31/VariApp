using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Migrations;

/// <summary>
/// Finaliza la transición de los maestros normalizados creada en M1.
/// El backfill inicial preservó los IDs de CatalogosProducto mediante claves explícitas;
/// una vez preservadas esas referencias, las nuevas altas deben usar AUTO_INCREMENT.
/// MySQL conserva los IDs existentes y continúa desde MAX(Id) + 1.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260809134500_M1HabilitarIdentidadesMaestros")]
public sealed class M1HabilitarIdentidadesMaestros : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE `Marcas` MODIFY COLUMN `Id` int NOT NULL AUTO_INCREMENT;");
        migrationBuilder.Sql("ALTER TABLE `Modelos` MODIFY COLUMN `Id` int NOT NULL AUTO_INCREMENT;");
        migrationBuilder.Sql("ALTER TABLE `Colores` MODIFY COLUMN `Id` int NOT NULL AUTO_INCREMENT;");
        migrationBuilder.Sql("ALTER TABLE `Tallas` MODIFY COLUMN `Id` int NOT NULL AUTO_INCREMENT;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE `Modelos` MODIFY COLUMN `Id` int NOT NULL;");
        migrationBuilder.Sql("ALTER TABLE `Marcas` MODIFY COLUMN `Id` int NOT NULL;");
        migrationBuilder.Sql("ALTER TABLE `Colores` MODIFY COLUMN `Id` int NOT NULL;");
        migrationBuilder.Sql("ALTER TABLE `Tallas` MODIFY COLUMN `Id` int NOT NULL;");
    }
}
