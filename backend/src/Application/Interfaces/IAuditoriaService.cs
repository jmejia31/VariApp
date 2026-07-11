using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Interfaces;

public interface IAuditoriaService
{
    /// Registra una entrada en la bitácora. No lanza excepciones si falla el guardado
    /// del log en sí (una falla de auditoría nunca debe tumbar la operación de negocio);
    /// el error se registra solo en el logger de la aplicación.
    Task RegistrarAsync(ModuloSistema modulo, AccionPermiso accion, string descripcion, int? referenciaId = null);

    Task<PagedResult<RegistroAuditoriaDto>> GetFilteredAsync(AuditoriaFiltroDto filtro);
}
