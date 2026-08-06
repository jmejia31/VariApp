-- Preflight check para validar que no existan múltiples registros predeterminados activos antes de aplicar la migración AddTipoClientePredeterminadoUnico
DROP PROCEDURE IF EXISTS CheckTipoClienteDuplicates;
DELIMITER //
CREATE PROCEDURE CheckTipoClienteDuplicates()
BEGIN
    DECLARE pred_count INT;
    SELECT COUNT(*) INTO pred_count FROM TipoClientes WHERE EsPredeterminado = 1 AND Activo = 1 AND Eliminado = 0;
    IF pred_count > 1 THEN
        SIGNAL SQLSTATE '45000' 
        SET MESSAGE_TEXT = 'Error Preflight: Existen múltiples TipoCliente predeterminados activos no eliminados en la base de datos. Corrígelo manualmente antes de migrar.';
    END IF;
END //
DELIMITER ;

CALL CheckTipoClienteDuplicates();
DROP PROCEDURE IF EXISTS CheckTipoClienteDuplicates;
