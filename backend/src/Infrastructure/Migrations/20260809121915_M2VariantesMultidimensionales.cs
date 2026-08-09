using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M2VariantesMultidimensionales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductoVariantes_CatalogosProducto_ColorId",
                table: "ProductoVariantes");

            migrationBuilder.DropIndex(
                name: "IX_ProductoVariantes_ProductoId_ColorId",
                table: "ProductoVariantes");

            migrationBuilder.AddColumn<int>(
                name: "MarcaId",
                table: "ProductoVariantes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModeloId",
                table: "ProductoVariantes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TallaId",
                table: "ProductoVariantes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentidadActivaUnica",
                table: "ProductoVariantes",
                type: "varchar(160)",
                maxLength: 160,
                nullable: true,
                computedColumnSql: "CASE WHEN `Eliminado` = 0 THEN CONCAT(`ProductoId`, ':', COALESCE(`MarcaId`, 0), ':', COALESCE(`ModeloId`, 0), ':', COALESCE(`ColorId`, 0), ':', COALESCE(`TallaId`, 0)) ELSE NULL END",
                stored: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariantes_Dimensiones",
                table: "ProductoVariantes",
                columns: new[] { "ProductoId", "MarcaId", "ModeloId", "ColorId", "TallaId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariantes_MarcaId",
                table: "ProductoVariantes",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariantes_ModeloId",
                table: "ProductoVariantes",
                column: "ModeloId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariantes_TallaId",
                table: "ProductoVariantes",
                column: "TallaId");

            migrationBuilder.CreateIndex(
                name: "UX_ProductoVariantes_IdentidadActiva",
                table: "ProductoVariantes",
                column: "IdentidadActivaUnica",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductoVariantes_Colores_ColorId",
                table: "ProductoVariantes",
                column: "ColorId",
                principalTable: "Colores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductoVariantes_Marcas_MarcaId",
                table: "ProductoVariantes",
                column: "MarcaId",
                principalTable: "Marcas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductoVariantes_Modelos_ModeloId",
                table: "ProductoVariantes",
                column: "ModeloId",
                principalTable: "Modelos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductoVariantes_Tallas_TallaId",
                table: "ProductoVariantes",
                column: "TallaId",
                principalTable: "Tallas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductoVariantes_Colores_ColorId",
                table: "ProductoVariantes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductoVariantes_Marcas_MarcaId",
                table: "ProductoVariantes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductoVariantes_Modelos_ModeloId",
                table: "ProductoVariantes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductoVariantes_Tallas_TallaId",
                table: "ProductoVariantes");

            migrationBuilder.DropIndex(
                name: "IX_ProductoVariantes_Dimensiones",
                table: "ProductoVariantes");

            migrationBuilder.DropIndex(
                name: "IX_ProductoVariantes_MarcaId",
                table: "ProductoVariantes");

            migrationBuilder.DropIndex(
                name: "IX_ProductoVariantes_ModeloId",
                table: "ProductoVariantes");

            migrationBuilder.DropIndex(
                name: "IX_ProductoVariantes_TallaId",
                table: "ProductoVariantes");

            migrationBuilder.DropIndex(
                name: "UX_ProductoVariantes_IdentidadActiva",
                table: "ProductoVariantes");

            migrationBuilder.DropColumn(
                name: "IdentidadActivaUnica",
                table: "ProductoVariantes");

            migrationBuilder.DropColumn(
                name: "MarcaId",
                table: "ProductoVariantes");

            migrationBuilder.DropColumn(
                name: "ModeloId",
                table: "ProductoVariantes");

            migrationBuilder.DropColumn(
                name: "TallaId",
                table: "ProductoVariantes");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariantes_ProductoId_ColorId",
                table: "ProductoVariantes",
                columns: new[] { "ProductoId", "ColorId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductoVariantes_CatalogosProducto_ColorId",
                table: "ProductoVariantes",
                column: "ColorId",
                principalTable: "CatalogosProducto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
