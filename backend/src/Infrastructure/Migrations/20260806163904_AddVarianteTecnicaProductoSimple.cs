using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVarianteTecnicaProductoSimple : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsTecnica",
                table: "ProductoVariantes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ProductoTecnicoUnico",
                table: "ProductoVariantes",
                type: "int",
                nullable: true,
                computedColumnSql: "CASE WHEN `EsTecnica` = 1 AND `Eliminado` = 0 THEN `ProductoId` ELSE NULL END",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariantes_ProductoTecnicoUnico",
                table: "ProductoVariantes",
                column: "ProductoTecnicoUnico",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductoVariantes_ProductoTecnicoUnico",
                table: "ProductoVariantes");

            migrationBuilder.DropColumn(
                name: "ProductoTecnicoUnico",
                table: "ProductoVariantes");

            migrationBuilder.DropColumn(
                name: "EsTecnica",
                table: "ProductoVariantes");
        }
    }
}
