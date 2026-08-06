-- Preflight check para validar que no existan múltiples TipoCliente predeterminados activos sin crear objetos residuales.
SELECT IF((SELECT COUNT(*) FROM TipoClientes WHERE EsPredeterminado = 1 AND Activo = 1 AND Eliminado = 0) > 1, 
          (SELECT 1 FROM `Existen_Multiples_TipoCliente_Predeterminados_Activos_Favor_Corregir_Manualmente`), 
          1) AS PreflightCheckStatus;
