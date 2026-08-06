using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryApp.Infrastructure.Services;

public class InventarioConcurrencyService : IInventarioConcurrencyService
{
    private readonly AppDbContext _context;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;

    public InventarioConcurrencyService(
        AppDbContext context,
        IProductoRepository productoRepository,
        IProductoVarianteRepository productoVarianteRepository)
    {
        _context = context;
        _productoRepository = productoRepository;
        _productoVarianteRepository = productoVarianteRepository;
    }

    public async Task<InventarioLockSet> BloquearYValidarInventarioAsync(
        IEnumerable<InventarioDemanda> demandMap,
        bool esDeduccion = true)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("BloquearYValidarInventarioAsync requiere una transacción activa.");

        var consolidada = demandMap
            .Select(x => x ?? throw new ArgumentException("La demanda de inventario contiene un elemento nulo.", nameof(demandMap)))
            .GroupBy(x => (x.ProductoId, x.ProductoVarianteId))
            .Select(g => new InventarioDemanda(
                g.Key.ProductoId,
                g.Key.ProductoVarianteId,
                g.Sum(x => x.Cantidad)))
            .OrderBy(x => x.ProductoId)
            .ThenBy(x => x.ProductoVarianteId)
            .ToList();

        if (consolidada.Any(x => x.ProductoId <= 0 || x.Cantidad <= 0))
            throw new BusinessRuleException("Cada demanda de inventario debe indicar un producto válido y una cantidad mayor a cero.");

        if (consolidada.Count == 0)
        {
            return new InventarioLockSet(
                new Dictionary<int, InventoryApp.Domain.Entities.Producto>(),
                new Dictionary<int, InventoryApp.Domain.Entities.ProductoVariante>(),
                Array.Empty<InventarioDemanda>());
        }

        var productoIds = consolidada
            .Select(x => x.ProductoId)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var productosMap = (await _productoRepository.GetByIdsForUpdateAsync(productoIds))
            .ToDictionary(p => p.Id);

        var varianteIds = consolidada
            .Where(x => x.ProductoVarianteId.HasValue)
            .Select(x => x.ProductoVarianteId!.Value)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var variantesMap = (await _productoVarianteRepository.GetByIdsForUpdateAsync(varianteIds))
            .ToDictionary(v => v.Id);

        foreach (var productoGrupo in consolidada.GroupBy(x => x.ProductoId))
        {
            if (!productosMap.TryGetValue(productoGrupo.Key, out var producto))
                throw new BusinessRuleException($"El producto ID '{productoGrupo.Key}' no existe o fue eliminado.");

            var cantidadTotalProducto = productoGrupo.Sum(x => x.Cantidad);
            if (esDeduccion && producto.Cantidad < cantidadTotalProducto)
            {
                throw new BusinessRuleException(
                    $"Stock insuficiente para '{producto.Nombre}': disponible {producto.Cantidad}, solicitado {cantidadTotalProducto}.");
            }
        }

        foreach (var item in consolidada)
        {
            var producto = productosMap[item.ProductoId];

            if (item.ProductoVarianteId.HasValue)
            {
                if (!variantesMap.TryGetValue(item.ProductoVarianteId.Value, out var variante))
                    throw new BusinessRuleException($"La variante ID '{item.ProductoVarianteId.Value}' no existe o fue eliminada.");

                if (variante.ProductoId != producto.Id)
                    throw new BusinessRuleException($"La variante ID '{variante.Id}' no pertenece al producto ID '{producto.Id}'.");

                if (esDeduccion && variante.Cantidad < item.Cantidad)
                {
                    throw new BusinessRuleException(
                        $"Stock insuficiente para la variante '{variante.Sku}': disponible {variante.Cantidad}, solicitado {item.Cantidad}.");
                }
            }
        }

        return new InventarioLockSet(productosMap, variantesMap, consolidada);
    }

    public async Task AjustarStockPesimistaAsync(
        int productoId,
        int? productoVarianteId,
        int cantidadActualEsperada,
        int cantidadNueva)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("AjustarStockPesimistaAsync requiere una transacción activa.");

        if (cantidadActualEsperada < 0 || cantidadNueva < 0)
            throw new BusinessRuleException("Las cantidades de inventario no pueden ser negativas.");

        var producto = await _productoRepository.GetByIdForUpdateAsync(productoId)
            ?? throw new BusinessRuleException($"El producto ID '{productoId}' no existe.");

        if (productoVarianteId.HasValue)
        {
            var variante = await _productoVarianteRepository.GetByIdForUpdateAsync(productoVarianteId.Value)
                ?? throw new BusinessRuleException($"La variante ID '{productoVarianteId.Value}' no existe.");

            if (variante.ProductoId != productoId)
                throw new BusinessRuleException("La variante indicada no pertenece al producto solicitado.");

            if (variante.Cantidad != cantidadActualEsperada)
            {
                throw new BusinessRuleException(
                    "El inventario cambió desde que se cargó el formulario. Actualiza los datos e inténtalo nuevamente.");
            }

            variante.Cantidad = cantidadNueva;
            _productoVarianteRepository.Update(variante);

            var todasVariantes = await _productoVarianteRepository.GetByProductoIdAsync(productoId, incluirInactivas: true);
            producto.Cantidad = todasVariantes.Sum(v => v.Cantidad);
            _productoRepository.Update(producto);
        }
        else
        {
            var variantesExistentes = await _productoVarianteRepository
                .GetByProductoIdAsync(productoId, incluirInactivas: true);
            if (variantesExistentes.Count > 0)
            {
                throw new BusinessRuleException(
                    "El producto tiene variantes. Ajusta el inventario de cada variante; el stock total se recalcula automáticamente.");
            }

            if (producto.Cantidad != cantidadActualEsperada)
            {
                throw new BusinessRuleException(
                    "El inventario cambió desde que se cargó el formulario. Actualiza los datos e inténtalo nuevamente.");
            }

            producto.Cantidad = cantidadNueva;
            _productoRepository.Update(producto);
        }
    }
}
