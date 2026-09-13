using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IThreeWayMatchService
{
    Task<ThreeWayMatchResultDto> EvaluarAsync(int ordenCompraId, CancellationToken cancellationToken = default);
}
