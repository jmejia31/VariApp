using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Migrations;

/// <summary>
/// ERP-N0.6 C3: certifica la integridad histórica del backfill tipado y
/// establece la exclusividad permanente del origen de MovimientoInventario.
/// Las columnas legacy ReferenciaTipo/ReferenciaId permanecen durante la
/// transición y se retirarán únicamente en N0.8 cuando corresponda.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260812084900_N0_6_C3_IntegridadOrigenTipadoMovimientoInventario")]
public sealed class N0_6_C3_IntegridadOrigenTipadoMovimientoInventario : Migration
{
    private const string ConstraintOrigenExclusivo =
        "CK_MovimientosInventario_OrigenTipado_Exclusivo_N06";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Fail-closed antes de instalar el constraint permanente. C2 ya debe
        // haber dejado exactamente una FK tipada por fila; C3 vuelve a comprobar
        // el estado real para evitar consolidar datos incoherentes.
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N06C3Guard;
            CREATE TEMPORARY TABLE __N06C3Guard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N06C3_Guard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N06C3Guard (Id, Violaciones)
            SELECT 1, COUNT(*)
              FROM MovimientosInventario m
             WHERE (m.CompraId IS NOT NULL)
                 + (m.VentaId IS NOT NULL)
                 + (m.ConsumoInsumoId IS NOT NULL) <> 1;

            INSERT INTO __N06C3Guard (Id, Violaciones)
            SELECT 2, COUNT(*)
              FROM MovimientosInventario m
             WHERE (CAST(m.ReferenciaTipo AS BINARY) IN (
                        CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY))
                    AND (m.CompraId IS NULL OR m.CompraId <> m.ReferenciaId
                         OR m.VentaId IS NOT NULL OR m.ConsumoInsumoId IS NOT NULL))
                OR (CAST(m.ReferenciaTipo AS BINARY) IN (
                        CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY))
                    AND (m.VentaId IS NULL OR m.VentaId <> m.ReferenciaId
                         OR m.CompraId IS NOT NULL OR m.ConsumoInsumoId IS NOT NULL))
                OR (CAST(m.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY)
                    AND (m.ConsumoInsumoId IS NULL OR m.ConsumoInsumoId <> m.ReferenciaId
                         OR m.CompraId IS NOT NULL OR m.VentaId IS NOT NULL));

            DROP TEMPORARY TABLE __N06C3Guard;
            """);

        migrationBuilder.Sql($"""
            ALTER TABLE MovimientosInventario
            ADD CONSTRAINT {ConstraintOrigenExclusivo}
            CHECK (
                (CompraId IS NOT NULL)
              + (VentaId IS NOT NULL)
              + (ConsumoInsumoId IS NOT NULL) = 1
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            ALTER TABLE MovimientosInventario
            DROP CHECK {ConstraintOrigenExclusivo};
            """);
    }
}
