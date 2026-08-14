-- ERP-N0.8.A — Preflight de migraciones y limpieza
-- Objetivo: producir evidencia de deuda legacy, históricos y dependencias antes de
-- cualquier eliminación física. Este script es estrictamente de solo lectura.
-- Compatible con MySQL 8.x. Ejecutar contra la base de DESARROLLO.

SET @schema_name := DATABASE();

SELECT
    'N0.8_PRECHECK_CONTEXT' AS check_id,
    @schema_name AS schema_name,
    VERSION() AS mysql_version,
    @@sql_mode AS sql_mode,
    @@sql_require_primary_key AS sql_require_primary_key;

-- 1) Inventario de tablas potencialmente afectadas por saneamiento ERP-N0.
SELECT
    'N0.8_TABLE_INVENTORY' AS check_id,
    t.TABLE_NAME,
    t.ENGINE,
    t.TABLE_ROWS,
    t.CREATE_TIME,
    t.UPDATE_TIME
FROM information_schema.TABLES t
WHERE t.TABLE_SCHEMA = @schema_name
  AND (
      LOWER(t.TABLE_NAME) REGEXP 'metodo.*pago|rol.*permiso|producto.*variante|movimiento.*inventario|ajuste.*inventario|venta|compra|auditor'
  )
ORDER BY t.TABLE_NAME;

-- 2) Columnas legacy/de compatibilidad que requieren decisión explícita antes de retiro.
SELECT
    'N0.8_LEGACY_COLUMNS' AS check_id,
    c.TABLE_NAME,
    c.COLUMN_NAME,
    c.COLUMN_TYPE,
    c.IS_NULLABLE,
    c.COLUMN_DEFAULT,
    c.COLUMN_KEY,
    c.EXTRA
FROM information_schema.COLUMNS c
WHERE c.TABLE_SCHEMA = @schema_name
  AND (
      LOWER(c.COLUMN_NAME) IN (
          'metodopago', 'metodopagoid', 'esadministrador', 'rol', 'modulo',
          'accion', 'permitido', 'productoatributoid', 'ajusteinventarioid'
      )
      OR LOWER(c.COLUMN_NAME) REGEXP 'metodo.*pago|legacy|histor|ajuste.*inventario|origen.*id'
  )
ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION;

-- 3) Foreign keys: ninguna columna/tabla puede retirarse si aún sostiene referencias.
SELECT
    'N0.8_FOREIGN_KEYS' AS check_id,
    kcu.TABLE_NAME,
    kcu.COLUMN_NAME,
    kcu.CONSTRAINT_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    rc.UPDATE_RULE,
    rc.DELETE_RULE
FROM information_schema.KEY_COLUMN_USAGE kcu
LEFT JOIN information_schema.REFERENTIAL_CONSTRAINTS rc
  ON rc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA
 AND rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
 AND rc.TABLE_NAME = kcu.TABLE_NAME
WHERE kcu.TABLE_SCHEMA = @schema_name
  AND kcu.REFERENCED_TABLE_NAME IS NOT NULL
  AND (
      LOWER(kcu.TABLE_NAME) REGEXP 'metodo.*pago|rol.*permiso|producto.*variante|movimiento.*inventario|ajuste.*inventario|venta|compra|auditor'
      OR LOWER(kcu.REFERENCED_TABLE_NAME) REGEXP 'metodo.*pago|rol.*permiso|producto.*variante|movimiento.*inventario|ajuste.*inventario|venta|compra|auditor'
  )
ORDER BY kcu.TABLE_NAME, kcu.COLUMN_NAME;

-- 4) Índices/PK de tablas candidatas. El preflight falla conceptualmente si una tabla
-- histórica o temporal persistida carece de clave primaria bajo sql_require_primary_key.
SELECT
    'N0.8_INDEXES' AS check_id,
    s.TABLE_NAME,
    s.INDEX_NAME,
    s.NON_UNIQUE,
    GROUP_CONCAT(s.COLUMN_NAME ORDER BY s.SEQ_IN_INDEX SEPARATOR ',') AS columns_in_index
FROM information_schema.STATISTICS s
WHERE s.TABLE_SCHEMA = @schema_name
  AND LOWER(s.TABLE_NAME) REGEXP 'metodo.*pago|rol.*permiso|producto.*variante|movimiento.*inventario|ajuste.*inventario|venta|compra|auditor'
GROUP BY s.TABLE_NAME, s.INDEX_NAME, s.NON_UNIQUE
ORDER BY s.TABLE_NAME, s.INDEX_NAME;

-- 5) Migraciones aplicadas: evidencia para reconciliar código vs. esquema antes de backfill/rollback.
SET @has_ef_history := (
    SELECT COUNT(*)
    FROM information_schema.TABLES
    WHERE TABLE_SCHEMA = @schema_name
      AND TABLE_NAME = '__EFMigrationsHistory'
);

SELECT
    'N0.8_EF_HISTORY_PRESENT' AS check_id,
    @has_ef_history AS present;

-- La lectura dinámica evita fallar cuando la tabla aún no existe en una BD vacía.
SET @sql := IF(
    @has_ef_history > 0,
    'SELECT ''N0.8_EF_HISTORY'' AS check_id, MigrationId, ProductVersion FROM `__EFMigrationsHistory` ORDER BY MigrationId',
    'SELECT ''N0.8_EF_HISTORY'' AS check_id, ''MISSING'' AS MigrationId, NULL AS ProductVersion'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 6) Triggers y vistas pueden mantener dependencias invisibles para un DROP/rename.
SELECT
    'N0.8_TRIGGERS' AS check_id,
    tr.TRIGGER_NAME,
    tr.EVENT_OBJECT_TABLE,
    tr.EVENT_MANIPULATION,
    tr.ACTION_TIMING
FROM information_schema.TRIGGERS tr
WHERE tr.TRIGGER_SCHEMA = @schema_name
  AND LOWER(tr.EVENT_OBJECT_TABLE) REGEXP 'metodo.*pago|rol.*permiso|producto.*variante|movimiento.*inventario|ajuste.*inventario|venta|compra|auditor'
ORDER BY tr.EVENT_OBJECT_TABLE, tr.TRIGGER_NAME;

SELECT
    'N0.8_VIEWS' AS check_id,
    v.TABLE_NAME AS VIEW_NAME,
    v.IS_UPDATABLE,
    v.SECURITY_TYPE
FROM information_schema.VIEWS v
WHERE v.TABLE_SCHEMA = @schema_name
  AND LOWER(v.VIEW_DEFINITION) REGEXP 'metodo.*pago|rol.*permiso|producto.*variante|movimiento.*inventario|ajuste.*inventario|venta|compra|auditor'
ORDER BY v.TABLE_NAME;

-- 7) Resultado contractual del preflight. No ejecuta DDL/DML destructivo.
SELECT
    'N0.8_PREFLIGHT_RESULT' AS check_id,
    CASE WHEN @schema_name IS NULL OR @schema_name = '' THEN 'FAIL' ELSE 'PASS' END AS result,
    CASE
        WHEN @schema_name IS NULL OR @schema_name = '' THEN 'No hay base seleccionada.'
        ELSE 'Inventario generado. Revisar históricos, FKs, índices, migraciones, triggers y vistas antes de cualquier limpieza física.'
    END AS detail;
