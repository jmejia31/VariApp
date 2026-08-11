using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class N0_2_RetirarCatalogoProductoLegacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_CatalogosProducto_ColorId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_CatalogosProducto_MarcaId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_CatalogosProducto_ModeloId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_CatalogosProducto_TallaId",
                table: "Productos");

            migrationBuilder.DropTable(
                name: "CatalogosProducto");

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Colores_ColorId",
                table: "Productos",
                column: "ColorId",
                principalTable: "Colores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Marcas_MarcaId",
                table: "Productos",
                column: "MarcaId",
                principalTable: "Marcas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Modelos_ModeloId",
                table: "Productos",
                column: "ModeloId",
                principalTable: "Modelos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Tallas_TallaId",
                table: "Productos",
                column: "TallaId",
                principalTable: "Tallas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Colores_ColorId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Marcas_MarcaId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Modelos_ModeloId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Tallas_TallaId",
                table: "Productos");

            migrationBuilder.CreateTable(
                name: "CatalogosProducto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CatalogoPadreId = table.Column<int>(type: "int", nullable: true),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CodigoVisual = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    Descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogosProducto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogosProducto_CatalogosProducto_CatalogoPadreId",
                        column: x => x.CatalogoPadreId,
                        principalTable: "CatalogosProducto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogosProducto_CatalogoPadreId",
                table: "CatalogosProducto",
                column: "CatalogoPadreId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogosProducto_Estado",
                table: "CatalogosProducto",
                columns: new[] { "Tipo", "Activo", "Eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogosProducto_Tipo_Nombre_Padre",
                table: "CatalogosProducto",
                columns: new[] { "Tipo", "Nombre", "CatalogoPadreId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_CatalogosProducto_ColorId",
                table: "Productos",
                column: "ColorId",
                principalTable: "CatalogosProducto",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_CatalogosProducto_MarcaId",
                table: "Productos",
                column: "MarcaId",
                principalTable: "CatalogosProducto",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_CatalogosProducto_ModeloId",
                table: "Productos",
                column: "ModeloId",
                principalTable: "CatalogosProducto",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_CatalogosProducto_TallaId",
                table: "Productos",
                column: "TallaId",
                principalTable: "CatalogosProducto",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
