using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Persistence.Migrations;

/// <summary>
/// ERP-N1.6.C: materializa el origen relacional tipado de Transferencia en Kardex.
/// Mantiene nullable el FK para preservar movimientos históricos y usa RESTRICT
/// para impedir que una transferencia con trazabilidad sea eliminada físicamente.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260816051000_N1_6_TransferenciaKardexOrigen")]
public sealed class N1_6_TransferenciaKardexOrigen : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "TransferenciaInventarioId",
            table: "MovimientosInventario",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_MovimientosInventario_TransferenciaInventarioId",
            table: "MovimientosInventario",
            column: "TransferenciaInventarioId");

        migrationBuilder.CreateIndex(
            name: "IX_MovInv_Transferencia_Fecha_N16",
            table: "MovimientosInventario",
            columns: new[] { "TransferenciaInventarioId", "Fecha" });

        migrationBuilder.AddForeignKey(
            name: "FK_MovimientosInventario_TransferenciasInventario_TransferenciaInventarioId",
            table: "MovimientosInventario",
            column: "TransferenciaInventarioId",
            principalTable: "TransferenciasInventario",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_MovimientosInventario_TransferenciasInventario_TransferenciaInventarioId",
            table: "MovimientosInventario");

        migrationBuilder.DropIndex(
            name: "IX_MovInv_Transferencia_Fecha_N16",
            table: "MovimientosInventario");

        migrationBuilder.DropIndex(
            name: "IX_MovimientosInventario_TransferenciaInventarioId",
            table: "MovimientosInventario");

        migrationBuilder.DropColumn(
            name: "TransferenciaInventarioId",
            table: "MovimientosInventario");
    }
}
