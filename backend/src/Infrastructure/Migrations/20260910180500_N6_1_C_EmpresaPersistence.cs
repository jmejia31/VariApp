using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260910180500_N6_1_C_EmpresaPersistence")]
public partial class N6_1_C_EmpresaPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Empresas",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Nombre = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Activa = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Empresas", x => x.Id);
                table.CheckConstraint("CK_Empresas_Nombre_NoVacio", "CHAR_LENGTH(TRIM(`Nombre`)) > 0");
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_Empresas_Activa_Nombre",
            table: "Empresas",
            columns: new[] { "Activa", "Nombre" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Empresas");
    }
}
