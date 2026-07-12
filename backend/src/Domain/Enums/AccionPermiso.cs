namespace InventoryApp.Domain.Enums;

public enum AccionPermiso
{
    Ver = 1,
    Crear = 2,
    Editar = 3,
    /// Conservado por compatibilidad con datos existentes; nuevas asignaciones
    /// deben usar EliminarLogico o EliminarPermanente.
    Eliminar = 4,
    Confirmar = 5,
    Anular = 6,
    Actualizar = 7,
    Activar = 8,
    Desactivar = 9,
    EliminarLogico = 10,
    EliminarPermanente = 11,
    Aprobar = 12,
    Rechazar = 13,
    Exportar = 14,
    Imprimir = 15,
    Administrar = 16,
    AsignarRol = 17,
    RestablecerContrasena = 18,
    CambiarEstado = 19,
    ConsultarHistorial = 20,
    Aplicar = 21,
    Duplicar = 22
}
