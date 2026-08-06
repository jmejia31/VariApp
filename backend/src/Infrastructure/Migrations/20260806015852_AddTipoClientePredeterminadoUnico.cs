using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoClientePredeterminadoUnico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP PROCEDURE IF EXISTS CheckTipoClienteDuplicates;
CREATE PROCEDURE CheckTipoClienteDuplicates()
BEGIN
    DECLARE pred_count INT;
    SELECT COUNT(*) INTO pred_count FROM TipoClientes WHERE EsPredeterminado = 1 AND Activo = 1 AND Eliminado = 0;
    IF pred_count > 1 THEN
        SIGNAL SQLSTATE '45000' 
        SET MESSAGE_TEXT = 'Error de Migración: Existen múltiples TipoCliente predeterminados activos no eliminados. Por favor, corrígelo manualmente.';
    END IF;
END;
");
            migrationBuilder.Sql("CALL CheckTipoClienteDuplicates();");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS CheckTipoClienteDuplicates;");

            migrationBuilder.AddColumn<string>(
                name: "EsPredeterminadoUnico",
                table: "TipoClientes",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true,
                computedColumnSql: "IF(EsPredeterminado = 1 AND Activo = 1 AND Eliminado = 0, 'DEFAULT', NULL)",
                stored: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TipoClientes_EsPredeterminadoUnico",
                table: "TipoClientes",
                column: "EsPredeterminadoUnico",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TipoClientes_EsPredeterminadoUnico",
                table: "TipoClientes");

            migrationBuilder.DropColumn(
                name: "EsPredeterminadoUnico",
                table: "TipoClientes");
        }
    }
}
