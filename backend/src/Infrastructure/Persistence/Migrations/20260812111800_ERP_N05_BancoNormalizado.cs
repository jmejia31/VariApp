using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260812111800_ERP_N05_BancoNormalizado")]
public partial class ERP_N05_BancoNormalizado : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Bancos",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Codigo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                CodigoNormalizado = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, computedColumnSql: "LOWER(TRIM(Codigo))", stored: true),
                Nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                SwiftBic = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Bancos", x => x.Id);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(name: "UX_Bancos_Codigo_Normalizado", table: "Bancos", column: "CodigoNormalizado", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Bancos_Nombre", table: "Bancos", column: "Nombre");
        migrationBuilder.CreateIndex(name: "IX_Bancos_Estado", table: "Bancos", columns: new[] { "Activo", "Eliminado" });

        migrationBuilder.AddColumn<int>(name: "BancoId", table: "FacturaPagos", type: "int", nullable: true);
        migrationBuilder.AddColumn<string>(name: "BancoCodigoSnapshot", table: "FacturaPagos", type: "varchar(50)", maxLength: 50, nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
        migrationBuilder.AddColumn<string>(name: "BancoNombreSnapshot", table: "FacturaPagos", type: "varchar(120)", maxLength: 120, nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(name: "IX_FacturaPagos_BancoId", table: "FacturaPagos", column: "BancoId");
        migrationBuilder.AddForeignKey(
            name: "FK_FacturaPagos_Bancos_BancoId",
            table: "FacturaPagos",
            column: "BancoId",
            principalTable: "Bancos",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_FacturaPagos_Bancos_BancoId", table: "FacturaPagos");
        migrationBuilder.DropIndex(name: "IX_FacturaPagos_BancoId", table: "FacturaPagos");
        migrationBuilder.DropColumn(name: "BancoId", table: "FacturaPagos");
        migrationBuilder.DropColumn(name: "BancoCodigoSnapshot", table: "FacturaPagos");
        migrationBuilder.DropColumn(name: "BancoNombreSnapshot", table: "FacturaPagos");
        migrationBuilder.DropTable(name: "Bancos");
    }
}
