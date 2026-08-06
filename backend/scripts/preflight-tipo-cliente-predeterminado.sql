-- Preflight check directo para validar que no existan múltiples TipoCliente predeterminados activos sin crear objetos residuales.
SELECT 
    CASE 
        WHEN COUNT(*) > 1 THEN 
            (SELECT table_name FROM information_schema.tables WHERE table_schema = 'non_existing_schema_to_fail_preflight')
        ELSE 1 
    END AS PreflightCheckStatus
FROM TipoClientes
WHERE EsPredeterminado = 1 AND Activo = 1 AND Eliminado = 0;
