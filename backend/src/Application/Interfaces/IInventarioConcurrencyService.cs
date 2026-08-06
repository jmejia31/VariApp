using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventoryApp.Application.Interfaces;

public interface IInventarioConcurrencyService
{
    Task BloquearYValidarInventarioAsync(
        IEnumerable<(int ProductoId, int? ProductoVarianteId, int Cantidad)> demandMap,
        bool esDeduccion = true);

    Task AjustarStockPesimistaAsync(
        int productoId,
        int? productoVarianteId,
        int cantidadActualEsperada,
        int cantidadNueva);
}
