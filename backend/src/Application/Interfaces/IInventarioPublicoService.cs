using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IInventarioPublicoService
{
    Task<IReadOnlyDictionary<int, InventarioPublicoVarianteDto>> ObtenerPorVariantesAsync(
        IEnumerable<int> productoVarianteIds);
}
