using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260809143000_M2ImagenesPorVariante")]
public sealed class M2ImagenesPorVariante : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ProductoVarianteId",
            table: "ProductoImagenes",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProductoImagenes_Producto_Variante_Orden",
            table: "ProductoImagenes",
            columns: new[] { "ProductoId", "ProductoVarianteId", "Orden" });

        migrationBuilder.CreateIndex(
            name: "IX_ProductoImagenes_Variante_Principal",
            table: "ProductoImagenes",
            columns: new[] { "ProductoVarianteId", "EsPrincipal" });

        migrationBuilder.AddForeignKey(
            name: "FK_ProductoImagenes_ProductoVariantes_ProductoVarianteId",
            table: "ProductoImagenes",
            column: "ProductoVarianteId",
            principalTable: "ProductoVariantes",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ProductoImagenes_ProductoVariantes_ProductoVarianteId",
            table: "ProductoImagenes");

        migrationBuilder.DropIndex(
            name: "IX_ProductoImagenes_Producto_Variante_Orden",
            table: "ProductoImagenes");

        migrationBuilder.DropIndex(
            name: "IX_ProductoImagenes_Variante_Principal",
            table: "ProductoImagenes");

        migrationBuilder.DropColumn(
            name: "ProductoVarianteId",
            table: "ProductoImagenes");
    }
}
