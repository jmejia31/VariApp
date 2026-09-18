using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;

namespace InventoryApp.Application.Services;

public sealed class InventarioPublicoService : IInventarioPublicoService
{
    private readonly IExistenciaVarianteRepository _repository;

    public InventarioPublicoService(IExistenciaVarianteRepository repository) =>
        _repository = repository;

    public async Task<IReadOnlyDictionary<int, InventarioPublicoVarianteDto>> ObtenerPorVariantesAsync(
        IEnumerable<int> productoVarianteIds)
    {
        var ids = productoVarianteIds.Where(id => id > 0).Distinct().OrderBy(id => id).ToArray();
        if (ids.Length == 0)
            return new Dictionary<int, InventarioPublicoVarianteDto>();

        var existencias = await _repository.GetRaizOperativaPorVariantesAsync(ids);
        var porVariante = existencias
            .GroupBy(e => e.ProductoVarianteId)
            .ToDictionary(g => g.Key, g =>
            {
                var disponible = (int)Math.Min(int.MaxValue, g.Sum(e => (long)Math.Max(0, e.StockDisponible)));
                var minimo = (int)Math.Min(int.MaxValue, g.Sum(e => (long)Math.Max(0, e.StockMinimo)));
                return new InventarioPublicoVarianteDto
                {
                    ProductoVarianteId = g.Key,
                    CantidadDisponible = disponible,
                    StockMinimo = minimo,
                    TieneStockBajo = disponible > 0 && disponible <= minimo,
                    EstaAgotada = disponible <= 0,
                    TieneFuenteAutoritativa = true
                };
            });

        foreach (var id in ids)
        {
            if (porVariante.ContainsKey(id))
                continue;

            porVariante[id] = new InventarioPublicoVarianteDto
            {
                ProductoVarianteId = id,
                CantidadDisponible = 0,
                StockMinimo = 0,
                TieneStockBajo = false,
                EstaAgotada = true,
                TieneFuenteAutoritativa = false
            };
        }

        return porVariante;
    }
}
