using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IInventarioAjusteService
{
    Task<AjusteStockResultadoDto> AjustarProductoAsync(
        int productoId,
        AjusteStockRequest request);

    Task<AjusteStockResultadoDto> AjustarVarianteAsync(
        int productoId,
        int varianteId,
        AjusteStockRequest request);
}
