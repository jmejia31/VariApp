using InventoryApp.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventoryApp.Application.Interfaces;

public sealed record InventarioDemanda(
    int ProductoId,
    int? ProductoVarianteId,
    int Cantidad);

public sealed class InventarioLockSet
{
    public InventarioLockSet(
        IReadOnlyDictionary<int, Producto> productos,
        IReadOnlyDictionary<int, ProductoVariante> variantes)
    {
        Productos = productos;
        Variantes = variantes;
    }

    public IReadOnlyDictionary<int, Producto> Productos { get; }
    public IReadOnlyDictionary<int, ProductoVariante> Variantes { get; }
}

public interface IInventarioConcurrencyService
{
    Task<InventarioLockSet> BloquearYValidarInventarioAsync(
        IEnumerable<InventarioDemanda> demandMap,
        bool esDeduccion = true);

    Task AjustarStockPesimistaAsync(
        int productoId,
        int? productoVarianteId,
        int cantidadActualEsperada,
        int cantidadNueva);
}
