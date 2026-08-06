using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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

    public async Task BloquearYValidarInventarioAsync(
        IEnumerable<(int ProductoId, int? ProductoVarianteId, int Cantidad)> demandMap,
        bool esDeduccion = true)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("BloquearYValidarInventarioAsync requiere una transacción activa.");

        var consolidatedList = demandMap
            .GroupBy(x => (x.ProductoId, x.ProductoVarianteId))
            .Select(g => (
                ProductoId: g.Key.ProductoId,
                ProductoVarianteId: g.Key.ProductoVarianteId,
                CantidadTotal: g.Sum(x => x.Cantidad)
            ))
            .ToList();

        if (consolidatedList.Count == 0) return;

        // 1. Bloquear productos ordenados por ProductoId ASC
        var productoIds = consolidatedList.Select(x => x.ProductoId).Distinct().OrderBy(id => id).ToList();
        var productosMap = (await _productoRepository.GetByIdsForUpdateAsync(productoIds))
            .ToDictionary(p => p.Id);

        // 2. Bloquear variantes ordenadas por ProductoVarianteId ASC
        var varianteIds = consolidatedList
            .Where(x => x.ProductoVarianteId.HasValue)
            .Select(x => x.ProductoVarianteId!.Value)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var variantesMap = (await _productoVarianteRepository.GetByIdsForUpdateAsync(varianteIds))
            .ToDictionary(v => v.Id);

        // 3. Validar existencias y mantener la invariante Producto.Cantidad = Sum(Variantes no eliminadas)
        foreach (var item in consolidatedList)
        {
            if (!productosMap.TryGetValue(item.ProductoId, out var producto))
                throw new BusinessRuleException($"El producto ID '{item.ProductoId}' no existe o fue eliminado.");

            if (item.ProductoVarianteId.HasValue)
            {
                if (!variantesMap.TryGetValue(item.ProductoVarianteId.Value, out var variante))
                    throw new BusinessRuleException($"La variante ID '{item.ProductoVarianteId.Value}' no existe o fue eliminada.");

                if (esDeduccion && variante.Cantidad < item.CantidadTotal)
                {
                    throw new BusinessRuleException(
                        $"Stock insuficiente para la variante '{variante.Sku}': disponible {variante.Cantidad}, solicitado {item.CantidadTotal}.");
                }
            }
            else
            {
                if (esDeduccion && producto.Cantidad < item.CantidadTotal)
                {
                    throw new BusinessRuleException(
                        $"Stock insuficiente para '{producto.Nombre}': disponible {producto.Cantidad}, solicitado {item.CantidadTotal}.");
                }
            }
        }
    }

    public async Task AjustarStockPesimistaAsync(
        int productoId,
        int? productoVarianteId,
        int cantidadActualEsperada,
        int cantidadNueva)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("AjustarStockPesimistaAsync requiere una transacción activa.");

        var producto = await _productoRepository.GetByIdForUpdateAsync(productoId)
            ?? throw new BusinessRuleException($"El producto ID '{productoId}' no existe.");

        if (productoVarianteId.HasValue)
        {
            var variante = await _productoVarianteRepository.GetByIdForUpdateAsync(productoVarianteId.Value)
                ?? throw new BusinessRuleException($"La variante ID '{productoVarianteId.Value}' no existe.");

            if (variante.Cantidad != cantidadActualEsperada)
            {
                throw new BusinessRuleException(
                    "El inventario cambió desde que se cargó el formulario. Actualiza los datos e inténtalo nuevamente.");
            }

            variante.Cantidad = cantidadNueva;
            _productoVarianteRepository.Update(variante);

            // Re-calcular invariante de producto sumando TODAS las variantes no eliminadas (incluidas inactivas)
            var todasVariantes = await _productoVarianteRepository.GetByProductoIdAsync(productoId, incluirInactivas: true);
            producto.Cantidad = todasVariantes.Sum(v => v.Cantidad);
            _productoRepository.Update(producto);
        }
        else
        {
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
