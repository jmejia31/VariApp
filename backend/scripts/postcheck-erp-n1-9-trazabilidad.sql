-- ERP-N1.9.C — Postcheck read-only para lotes, series y vencimientos.
-- Debe ejecutarse después de aplicar 20260817100000_N1_9_TrazabilidadLotesSeries.

SELECT 'Tablas N1.9 materializadas' AS CheckName,
       COUNT(*) = 2 AS Ok
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name IN ('LotesInventario', 'SeriesInventario');

SELECT 'Columnas opt-in materializadas' AS CheckName,
       COUNT(*) = 4 AS Ok
FROM information_schema.columns
WHERE table_schema = DATABASE()
  AND table_name = 'ProductoVariantes'
  AND column_name IN (
      'ControlaLote',
      'ControlaNumeroSerie',
      'ControlaFechaVencimiento',
      'DiasAlertaVencimiento'
  );

SELECT 'Índices únicos N1.9 presentes' AS CheckName,
       SUM(index_name = 'UX_LotesInventario_Variante_Codigo' AND non_unique = 0) >= 1
       AND SUM(index_name = 'UX_SeriesInventario_NumeroSerie' AND non_unique = 0) >= 1 AS Ok
FROM information_schema.statistics
WHERE table_schema = DATABASE()
  AND table_name IN ('LotesInventario', 'SeriesInventario');

SELECT 'FKs restrictivas N1.9 presentes' AS CheckName,
       COUNT(*) >= 3 AS Ok
FROM information_schema.referential_constraints
WHERE constraint_schema = DATABASE()
  AND constraint_name IN (
      'FK_LotesInventario_ProductoVariantes_ProductoVarianteId',
      'FK_SeriesInventario_ProductoVariantes_ProductoVarianteId',
      'FK_SeriesInventario_LotesInventario_LoteInventarioId'
  );

SELECT 'Baseline legacy conservado' AS CheckName,
       SUM(CASE WHEN ControlaLote = 0
                 AND ControlaNumeroSerie = 0
                 AND ControlaFechaVencimiento = 0
                 AND DiasAlertaVencimiento IS NULL THEN 1 ELSE 0 END) = COUNT(*) AS Ok
FROM ProductoVariantes;
