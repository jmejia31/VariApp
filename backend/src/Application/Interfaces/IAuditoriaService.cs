using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Interfaces;

public interface IAuditoriaService
{
    /// Registra una entrada en la bitácora. No lanza excepciones si falla el guardado
    /// del log en sí (una falla de auditoría nunca debe tumbar la operación de negocio);
    /// el error se registra solo en el logger de la aplicación.
    /// IP, UserAgent y CorrelationId se capturan automáticamente del HttpContext
    /// actual — los llamadores no necesitan pasarlos.
    Task RegistrarAsync(
        ModuloSistema modulo, AccionPermiso accion, string descripcion, int? referenciaId = null,
        string? entidad = null, object? valoresAnteriores = null, object? valoresNuevos = null,
        string? motivo = null, string resultado = "Exito", string? error = null);

    Task<PagedResult<RegistroAuditoriaDto>> GetFilteredAsync(AuditoriaFiltroDto filtro);
}
