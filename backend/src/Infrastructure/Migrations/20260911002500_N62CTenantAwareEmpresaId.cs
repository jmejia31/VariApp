using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Migrations;

/// <summary>
/// N6.2.C: convierte EmpresaId de Sucursales en ownership tenant-aware obligatorio.
/// El ALTER se ejecuta como una sola operación DDL para fallar cerrado: si existen
/// filas con EmpresaId NULL o referencias huérfanas, MySQL rechaza la operación en
/// vez de asignar silenciosamente una empresa arbitraria.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260911002500_N62CTenantAwareEmpresaId")]
public partial class N62CTenantAwareEmpresaId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE `Sucursales`
                MODIFY COLUMN `EmpresaId` int NOT NULL,
                ADD CONSTRAINT `FK_Sucursales_Empresas_EmpresaId`
                    FOREIGN KEY (`EmpresaId`) REFERENCES `Empresas` (`Id`) ON DELETE RESTRICT;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE `Sucursales`
                DROP FOREIGN KEY `FK_Sucursales_Empresas_EmpresaId`,
                MODIFY COLUMN `EmpresaId` int NULL;
            """);
    }
}
