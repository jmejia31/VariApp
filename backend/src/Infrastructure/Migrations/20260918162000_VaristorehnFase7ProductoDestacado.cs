using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260918162000_VaristorehnFase7ProductoDestacado")]
    public partial class VaristorehnFase7ProductoDestacado : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsDestacado",
                table: "Productos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_EsDestacado_Activo",
                table: "Productos",
                columns: new[] { "EsDestacado", "Activo" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Productos_EsDestacado_Activo",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "EsDestacado",
                table: "Productos");
        }
    }
}
