using InventoryApp.Application.DTOs.Contabilidad;

namespace InventoryApp.Application.Interfaces;

public interface IContabilizacionService
{
    Task<AsientoContableWriteResult> ContabilizarAsync(
        EventoContableDto evento,
        CancellationToken cancellationToken = default);
}
