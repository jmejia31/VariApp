from pathlib import Path
import re

MIGRATION_TEMPLATE = r'''using System;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Migrations
{
    /// <summary>
    /// ERP-N1.3 C: persistencia aditiva de la topología interna de Almacenes.
    /// No migra existencias, cantidades ni movimientos; esas autoridades pertenecen a ERP-N1.4+.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("__MIGRATION_ID__")]
    public partial class N1_3_UbicacionAlmacenPersistencia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N13CGuard;
                CREATE TEMPORARY TABLE __N13CGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N13C_Guard_Cero CHECK (Violaciones = 0)
                );

                INSERT INTO __N13CGuard (Id, Violaciones)
                SELECT 1, COUNT(*)
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen';

                INSERT INTO __N13CGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name = 'Almacenes';

                DROP TEMPORARY TABLE __N13CGuard;
                """);

            migrationBuilder.CreateTable(
                name: "UbicacionesAlmacen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    UbicacionPadreId = table.Column<int>(type: "int", nullable: true),
                    Codigo = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CodigoActivoUnico = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true, computedColumnSql: "IF(Eliminado = 0, UPPER(TRIM(Codigo)), NULL)", stored: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UbicacionesAlmacen", x => x.Id);
                    table.UniqueConstraint("AK_UbicacionesAlmacen_AlmacenId_Id", x => new { x.AlmacenId, x.Id });
                    table.ForeignKey(
                        name: "FK_UbicacionesAlmacen_Almacenes_AlmacenId",
                        column: x => x.AlmacenId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UbicacionesAlmacen_Padre_MismoAlmacen",
                        columns: x => new { x.AlmacenId, x.UbicacionPadreId },
                        principalTable: "UbicacionesAlmacen",
                        principalColumns: new[] { "AlmacenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UbicacionesAlmacen_AlmacenId",
                table: "UbicacionesAlmacen",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_UbicacionesAlmacen_Padre",
                table: "UbicacionesAlmacen",
                columns: new[] { "AlmacenId", "UbicacionPadreId" });

            migrationBuilder.CreateIndex(
                name: "IX_UbicacionesAlmacen_Tipo_Estado",
                table: "UbicacionesAlmacen",
                columns: new[] { "Tipo", "Activa", "Eliminado" });

            migrationBuilder.CreateIndex(
                name: "UX_UbicacionesAlmacen_Almacen_Codigo_Activo",
                table: "UbicacionesAlmacen",
                columns: new[] { "AlmacenId", "CodigoActivoUnico" },
                unique: true);

            migrationBuilder.Sql("""
                ALTER TABLE UbicacionesAlmacen
                    ADD CONSTRAINT CK_UbicacionesAlmacen_Codigo_NoVacio CHECK (CHAR_LENGTH(TRIM(Codigo)) > 0),
                    ADD CONSTRAINT CK_UbicacionesAlmacen_Nombre_NoVacio CHECK (CHAR_LENGTH(TRIM(Nombre)) > 0),
                    ADD CONSTRAINT CK_UbicacionesAlmacen_Tipo_Valido CHECK (Tipo BETWEEN 1 AND 6),
                    ADD CONSTRAINT CK_UbicacionesAlmacen_Padre_NoSelf CHECK (UbicacionPadreId IS NULL OR UbicacionPadreId <> Id);
                """);

            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N13CPostGuard;
                CREATE TEMPORARY TABLE __N13CPostGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N13C_PostGuard_Cero CHECK (Violaciones = 0)
                );

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen';

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(DISTINCT index_name) = 4 THEN 0 ELSE 1 END
                  FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND index_name IN (
                        'UX_UbicacionesAlmacen_Almacen_Codigo_Activo',
                        'IX_UbicacionesAlmacen_AlmacenId',
                        'IX_UbicacionesAlmacen_Padre',
                        'IX_UbicacionesAlmacen_Tipo_Estado');

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 3, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND index_name = 'UX_UbicacionesAlmacen_Almacen_Codigo_Activo'
                   AND non_unique = 0;

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 4, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.table_constraints
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND constraint_name = 'AK_UbicacionesAlmacen_AlmacenId_Id'
                   AND constraint_type = 'UNIQUE';

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 5, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.referential_constraints
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND constraint_name = 'FK_UbicacionesAlmacen_Almacenes_AlmacenId'
                   AND referenced_table_name = 'Almacenes'
                   AND delete_rule = 'RESTRICT';

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 6, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.referential_constraints
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND constraint_name = 'FK_UbicacionesAlmacen_Padre_MismoAlmacen'
                   AND referenced_table_name = 'UbicacionesAlmacen'
                   AND delete_rule = 'RESTRICT';

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 7, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.key_column_usage
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND constraint_name = 'FK_UbicacionesAlmacen_Padre_MismoAlmacen'
                   AND column_name IN ('AlmacenId', 'UbicacionPadreId');

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 8, CASE WHEN COUNT(*) = 4 THEN 0 ELSE 1 END
                  FROM information_schema.table_constraints
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND constraint_type = 'CHECK'
                   AND constraint_name IN (
                        'CK_UbicacionesAlmacen_Codigo_NoVacio',
                        'CK_UbicacionesAlmacen_Nombre_NoVacio',
                        'CK_UbicacionesAlmacen_Tipo_Valido',
                        'CK_UbicacionesAlmacen_Padre_NoSelf');

                DROP TEMPORARY TABLE __N13CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N13CDownGuard;
                CREATE TEMPORARY TABLE __N13CDownGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N13C_DownGuard_Cero CHECK (Violaciones = 0)
                );

                INSERT INTO __N13CDownGuard (Id, Violaciones)
                SELECT 1, COUNT(*) FROM UbicacionesAlmacen;

                DROP TEMPORARY TABLE __N13CDownGuard;
                """);

            migrationBuilder.DropTable(name: "UbicacionesAlmacen");
        }
    }
}
'''


def extract_entity_blocks(snapshot: str) -> list[str]:
    marker = 'modelBuilder.Entity("InventoryApp.Domain.Entities.UbicacionAlmacen", b =>'
    lines = snapshot.splitlines()
    starts = [i for i, line in enumerate(lines) if marker in line]
    if len(starts) != 3:
        raise RuntimeError(f'Se esperaban 3 bloques UbicacionAlmacen y se hallaron {len(starts)}')

    blocks: list[str] = []
    for start in starts:
        depth = 0
        begun = False
        end = None
        for i in range(start, len(lines)):
            line = lines[i]
            if '{' in line:
                begun = True
            depth += line.count('{') - line.count('}')
            if begun and depth == 0 and line.strip() == '});':
                end = i
                break
        if end is None:
            raise RuntimeError(f'No se pudo cerrar bloque desde línea {start + 1}')
        blocks.append('\n'.join(lines[start:end + 1]) + '\n\n')
    return blocks


def reconcile_snapshot(base_path: Path, generated_path: Path) -> None:
    base = base_path.read_text(encoding='utf-8-sig')
    generated = generated_path.read_text(encoding='utf-8-sig')
    if 'InventoryApp.Domain.Entities.UbicacionAlmacen' in base:
        raise RuntimeError('El snapshot base ya contiene UbicacionAlmacen; se rehúsa duplicar el delta')

    definition, relation, navigation = extract_entity_blocks(generated)

    almacen_marker = '            modelBuilder.Entity("InventoryApp.Domain.Entities.Almacen", b =>'
    almacen_positions = [m.start() for m in re.finditer(re.escape(almacen_marker), base)]
    if len(almacen_positions) != 2:
        raise RuntimeError(f'Se esperaban 2 bloques Almacen en snapshot condensado y se hallaron {len(almacen_positions)}')

    base = base[:almacen_positions[1]] + definition + base[almacen_positions[1]:]

    second_almacen = base.find(almacen_marker, base.find(almacen_marker) + 1)
    compra_marker = '            modelBuilder.Entity("InventoryApp.Domain.Entities.Compra", b =>'
    compra_relation = base.find(compra_marker, second_almacen)
    if compra_relation < 0:
        raise RuntimeError('No se encontró el bloque relacional de Compra para insertar la relación UbicacionAlmacen')
    base = base[:compra_relation] + relation + base[compra_relation:]

    close_anchor = '''        }\n    }\n\n    public partial class ERP_N05_PermiteCambioAuditable'''
    if close_anchor not in base:
        raise RuntimeError('No se encontró cierre canónico del snapshot condensado')
    base = base.replace(close_anchor, navigation + '        }\n    }\n\n    public partial class ERP_N05_PermiteCambioAuditable', 1)

    base_path.write_text(base, encoding='utf-8-sig')


def main() -> None:
    migrations = Path('backend/src/Infrastructure/Migrations')
    generated = sorted(migrations.glob('*_N1_3_UbicacionAlmacenPersistencia.cs'))
    generated = [p for p in generated if not p.name.endswith('.Designer.cs')]
    if len(generated) != 1:
        raise RuntimeError(f'Se esperaba una migración N1.3 generada y se hallaron {len(generated)}')

    migration_path = generated[0]
    migration_id = migration_path.stem
    migration_path.write_text(
        MIGRATION_TEMPLATE.replace('__MIGRATION_ID__', migration_id),
        encoding='utf-8-sig')

    designer = migration_path.with_name(migration_path.stem + '.Designer.cs')
    generated_snapshot = migrations / 'AppDbContextModelSnapshot.cs'
    base_snapshot = Path('/tmp/n13-base-snapshot.cs')
    reconcile_snapshot(base_snapshot, generated_snapshot)
    generated_snapshot.write_bytes(base_snapshot.read_bytes())

    if designer.exists():
        designer.unlink()

    print(f'Migración endurecida: {migration_path}')
    print('Snapshot condensado reconciliado con UbicacionAlmacen')


if __name__ == '__main__':
    main()
