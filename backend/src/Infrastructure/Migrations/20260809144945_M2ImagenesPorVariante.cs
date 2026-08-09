using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M2ImagenesPorVariante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductoVarianteId",
                table: "ProductoImagenes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrincipalAmbitoKey",
                table: "ProductoImagenes",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true,
                computedColumnSql: "IF(EsPrincipal = 1, CONCAT(ProductoId, ':', IFNULL(ProductoVarianteId, 0)), NULL)",
                stored: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Se crea primero un índice cuyo prefijo es ProductoId. MySQL lo
            // puede reutilizar para la FK existente ProductoImagenes->Productos;
            // solo entonces es seguro retirar el índice simple anterior.
            migrationBuilder.CreateIndex(
                name: "IX_ProductoImagenes_Producto_Variante_Orden",
                table: "ProductoImagenes",
                columns: new[] { "ProductoId", "ProductoVarianteId", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductoImagenes_ProductoVarianteId",
                table: "ProductoImagenes",
                column: "ProductoVarianteId");

            migrationBuilder.CreateIndex(
                name: "UX_ProductoImagenes_Principal_Ambito",
                table: "ProductoImagenes",
                column: "PrincipalAmbitoKey",
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_ProductoImagenes_ProductoId",
                table: "ProductoImagenes");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductoImagenes_ProductoVariantes_ProductoVarianteId",
                table: "ProductoImagenes",
                column: "ProductoVarianteId",
                principalTable: "ProductoVariantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductoImagenes_ProductoVariantes_ProductoVarianteId",
                table: "ProductoImagenes");

            // La FK hacia Productos necesita conservar un índice por ProductoId
            // antes de retirar el índice compuesto introducido por M2.
            migrationBuilder.CreateIndex(
                name: "IX_ProductoImagenes_ProductoId",
                table: "ProductoImagenes",
                column: "ProductoId");

            migrationBuilder.DropIndex(
                name: "IX_ProductoImagenes_Producto_Variante_Orden",
                table: "ProductoImagenes");

            migrationBuilder.DropIndex(
                name: "IX_ProductoImagenes_ProductoVarianteId",
                table: "ProductoImagenes");

            migrationBuilder.DropIndex(
                name: "UX_ProductoImagenes_Principal_Ambito",
                table: "ProductoImagenes");

            migrationBuilder.DropColumn(
                name: "PrincipalAmbitoKey",
                table: "ProductoImagenes");

            migrationBuilder.DropColumn(
                name: "ProductoVarianteId",
                table: "ProductoImagenes");
        }
    }
}
