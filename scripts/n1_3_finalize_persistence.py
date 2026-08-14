from pathlib import Path
import re

PRECHECK = '''            migrationBuilder.Sql("""
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

'''

POSTCHECK = '''
            migrationBuilder.Sql("""
                ALTER TABLE UbicacionesAlmacen
                    ADD CONSTRAINT CK_UbicacionesAlmacen_Codigo_NoVacio CHECK (CHAR_LENGTH(TRIM(Codigo)) > 0),
                    ADD CONSTRAINT CK_UbicacionesAlmacen_Nombre_NoVacio CHECK (CHAR_LENGTH(TRIM(Nombre)) > 0),
                    ADD CONSTRAINT CK_UbicacionesAlmacen_Tipo_Valido CHECK (Tipo BETWEEN 1 AND 6);
                """);

            // MySQL 8.4 no permite que un CHECK referencie Id AUTO_INCREMENT.
            // La misma invariante se protege físicamente con triggers fail-closed.
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_UbicacionesAlmacen_NoSelf_Insert
                AFTER INSERT ON UbicacionesAlmacen
                FOR EACH ROW
                BEGIN
                    IF NEW.UbicacionPadreId IS NOT NULL AND NEW.UbicacionPadreId = NEW.Id THEN
                        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Una ubicacion no puede ser su propio padre.';
                    END IF;
                END
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER TR_UbicacionesAlmacen_NoSelf_Update
                BEFORE UPDATE ON UbicacionesAlmacen
                FOR EACH ROW
                BEGIN
                    IF NEW.UbicacionPadreId IS NOT NULL AND NEW.UbicacionPadreId = NEW.Id THEN
                        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Una ubicacion no puede ser su propio padre.';
                    END IF;
                END
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
                SELECT 8, CASE WHEN COUNT(*) = 3 THEN 0 ELSE 1 END
                  FROM information_schema.table_constraints
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND constraint_type = 'CHECK'
                   AND constraint_name IN (
                        'CK_UbicacionesAlmacen_Codigo_NoVacio',
                        'CK_UbicacionesAlmacen_Nombre_NoVacio',
                        'CK_UbicacionesAlmacen_Tipo_Valido');

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 9, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.triggers
                 WHERE trigger_schema = DATABASE()
                   AND event_object_table = 'UbicacionesAlmacen'
                   AND trigger_name IN (
                        'TR_UbicacionesAlmacen_NoSelf_Insert',
                        'TR_UbicacionesAlmacen_NoSelf_Update');

                DROP TEMPORARY TABLE __N13CPostGuard;
                """);
'''

DOWN_GUARD = '''            migrationBuilder.Sql("""
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
    positions = [m.start() for m in re.finditer(re.escape(almacen_marker), base)]
    if len(positions) != 2:
        raise RuntimeError(f'Se esperaban 2 bloques Almacen y se hallaron {len(positions)}')

    base = base[:positions[1]] + definition + base[positions[1]:]
    second_almacen = base.find(almacen_marker, base.find(almacen_marker) + 1)
    compra_marker = '            modelBuilder.Entity("InventoryApp.Domain.Entities.Compra", b =>'
    compra_relation = base.find(compra_marker, second_almacen)
    if compra_relation < 0:
        raise RuntimeError('No se encontró bloque relacional de Compra')
    base = base[:compra_relation] + relation + base[compra_relation:]

    close_anchor = '        }\n    }\n\n    public partial class ERP_N05_PermiteCambioAuditable'
    if close_anchor not in base:
        raise RuntimeError('No se encontró cierre canónico del snapshot condensado')
    base = base.replace(
        close_anchor,
        navigation + '        }\n    }\n\n    public partial class ERP_N05_PermiteCambioAuditable',
        1)
    generated_path.write_text(base, encoding='utf-8-sig')


def harden_migration(path: Path) -> None:
    text = path.read_text(encoding='utf-8-sig')
    migration_id = path.stem

    text = text.replace(
        'using Microsoft.EntityFrameworkCore.Metadata;\n',
        'using InventoryApp.Infrastructure.Persistence;\nusing Microsoft.EntityFrameworkCore.Infrastructure;\nusing Microsoft.EntityFrameworkCore.Metadata;\n',
        1)
    class_marker = '    public partial class N1_3_UbicacionAlmacenPersistencia : Migration\n'
    if class_marker not in text:
        raise RuntimeError('No se encontró clase de migración generada')
    text = text.replace(
        class_marker,
        f'    [DbContext(typeof(AppDbContext))]\n    [Migration("{migration_id}")]\n' + class_marker,
        1)

    up_open = '        protected override void Up(MigrationBuilder migrationBuilder)\n        {\n'
    if up_open not in text:
        raise RuntimeError('No se encontró Up() generado')
    text = text.replace(up_open, up_open + PRECHECK, 1)

    up_close = '        }\n\n        /// <inheritdoc />\n        protected override void Down(MigrationBuilder migrationBuilder)\n'
    if up_close not in text:
        raise RuntimeError('No se encontró cierre de Up()')
    text = text.replace(
        up_close,
        POSTCHECK + '        }\n\n        /// <inheritdoc />\n        protected override void Down(MigrationBuilder migrationBuilder)\n',
        1)

    down_open = '        protected override void Down(MigrationBuilder migrationBuilder)\n        {\n'
    if down_open not in text:
        raise RuntimeError('No se encontró Down() generado')
    text = text.replace(down_open, down_open + DOWN_GUARD, 1)
    path.write_text(text, encoding='utf-8-sig')


def main() -> None:
    migrations = Path('backend/src/Infrastructure/Migrations')
    generated = [p for p in migrations.glob('*_N1_3_UbicacionAlmacenPersistencia.cs') if not p.name.endswith('.Designer.cs')]
    if len(generated) != 1:
        raise RuntimeError(f'Se esperaba una migración N1.3 generada y se hallaron {len(generated)}')

    migration = generated[0]
    snapshot = migrations / 'AppDbContextModelSnapshot.cs'
    base_snapshot = Path('/tmp/n13-base-snapshot.cs')

    harden_migration(migration)
    reconcile_snapshot(base_snapshot, snapshot)

    designer = migration.with_name(migration.stem + '.Designer.cs')
    if designer.exists():
        designer.unlink()

    print(f'Migración endurecida: {migration.name}')
    print('Self-parent: triggers físicos INSERT/UPDATE por limitación MySQL sobre AUTO_INCREMENT.')
    print('Snapshot condensado reconciliado con UbicacionAlmacen.')


if __name__ == '__main__':
    main()
