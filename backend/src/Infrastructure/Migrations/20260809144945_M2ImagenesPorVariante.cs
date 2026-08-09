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
            // MySQL 8.4 reconstruye ProductoImagenes al agregar una columna
            // calculada almacenada. La FK histórica a Productos es válida, pero
            // mantenerla activa durante esa reconstrucción provoca que InnoDB
            // falle al recrearla. Se retira y se restablece dentro de la misma
            // migración, preservando exactamente su semántica Cascade.
            migrationBuilder.DropForeignKey(
                name: "FK_ProductoImagenes_Productos_ProductoId",
                table: "ProductoImagenes");

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
                name: "FK_ProductoImagenes_Productos_ProductoId",
                table: "ProductoImagenes",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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

            migrationBuilder.DropForeignKey(
                name: "FK_ProductoImagenes_Productos_ProductoId",
                table: "ProductoImagenes");

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

            migrationBuilder.AddForeignKey(
                name: "FK_ProductoImagenes_Productos_ProductoId",
                table: "ProductoImagenes",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
