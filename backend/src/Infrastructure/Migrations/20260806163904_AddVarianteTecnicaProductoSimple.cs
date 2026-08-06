using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVarianteTecnicaProductoSimple : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsTecnica",
                table: "ProductoVariantes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ProductoTecnicoUnico",
                table: "ProductoVariantes",
                type: "int",
                nullable: true,
                computedColumnSql: "CASE WHEN `EsTecnica` = 1 AND `Eliminado` = 0 THEN `ProductoId` ELSE NULL END",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariantes_ProductoTecnicoUnico",
                table: "ProductoVariantes",
                column: "ProductoTecnicoUnico",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO ProductoVariantes
                    (ProductoId, ColorId, Sku, CodigoBarras, Cantidad, UmbralStockBajo,
                     Costo, Precio, Activo, Eliminado, FechaEliminacion, EliminadoPorUsuarioId,
                     FechaCreacion, FechaActualizacion, CreadoPorUsuarioId, CreadoPorNombreUsuario,
                     ActualizadoPorUsuarioId, ActualizadoPorNombreUsuario, EsTecnica)
                SELECT
                    p.Id,
                    NULL,
                    CONCAT('TEC-', LPAD(p.Id, 10, '0')),
                    NULL,
                    p.Cantidad,
                    p.UmbralStockBajo,
                    p.Costo,
                    p.Precio,
                    p.Activo,
                    0,
                    NULL,
                    NULL,
                    UTC_TIMESTAMP(6),
                    UTC_TIMESTAMP(6),
                    NULL,
                    'Migración Bloque 2C.1',
                    NULL,
                    NULL,
                    1
                FROM Productos p
                WHERE p.Eliminado = 0
                  AND NOT EXISTS (
                      SELECT 1
                      FROM ProductoVariantes pv
                      WHERE pv.ProductoId = p.Id
                        AND pv.Eliminado = 0
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Fail-closed: si una variante técnica ya tiene historial protegido,
            // las restricciones foráneas impedirán eliminarla y abortarán el rollback.
            migrationBuilder.Sql(
                """
                DELETE FROM ProductoVariantes
                WHERE EsTecnica = 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_ProductoVariantes_ProductoTecnicoUnico",
                table: "ProductoVariantes");

            migrationBuilder.DropColumn(
                name: "ProductoTecnicoUnico",
                table: "ProductoVariantes");

            migrationBuilder.DropColumn(
                name: "EsTecnica",
                table: "ProductoVariantes");
        }
    }
}
