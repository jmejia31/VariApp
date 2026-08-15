using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;

namespace InventoryApp.Infrastructure.Services;

/// <summary>
/// Unidad de concurrencia autoritativa de ERP-N1.4. No consulta ni valida
/// Producto.Cantidad/ProductoVariante.Cantidad: todo saldo vivo se protege sobre
/// ExistenciaVariante.
/// </summary>
public sealed class ExistenciaVarianteConcurrencyService : IExistenciaVarianteConcurrencyService
{
    private readonly AppDbContext _context;
    private readonly IExistenciaVarianteRepository _repository;

    public ExistenciaVarianteConcurrencyService(
        AppDbContext context,
        IExistenciaVarianteRepository repository)
    {
        _context = context;
        _repository = repository;
    }

    public Task<InventarioExistenciaLockSet> BloquearYValidarExistenciasAsync(
        IEnumerable<InventarioDemandaExistencia> demandas,
        bool esDeduccion = true) =>
        BloquearYValidarCoreAsync(demandas, esDeduccion, incluirEliminados: false);

    public Task<InventarioExistenciaLockSet> BloquearExistenciasParaReversionAsync(
        IEnumerable<InventarioDemandaExistencia> demandas) =>
        BloquearYValidarCoreAsync(demandas, esDeduccion: false, incluirEliminados: true);

    private async Task<InventarioExistenciaLockSet> BloquearYValidarCoreAsync(
        IEnumerable<InventarioDemandaExistencia> demandas,
        bool esDeduccion,
        bool incluirEliminados)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("El bloqueo de existencias requiere una transacción activa.");

        ArgumentNullException.ThrowIfNull(demandas);

        var materializadas = demandas
            .Select(d => d ?? throw new ArgumentException(
                "La demanda de existencias contiene un elemento nulo.",
                nameof(demandas)))
            .ToList();

        if (materializadas.Any(d =>
                d.ProductoId <= 0 ||
                d.ProductoVarianteId <= 0 ||
                d.AlmacenId <= 0 ||
                (d.UbicacionAlmacenId.HasValue && d.UbicacionAlmacenId.Value <= 0) ||
                d.Cantidad <= 0))
        {
            throw new BusinessRuleException(
                "Cada demanda debe indicar producto, variante y almacén válidos, ubicación positiva cuando aplique y cantidad mayor a cero.");
        }

        var consolidada = materializadas
            .GroupBy(d => d.Clave)
            .Select(g =>
            {
                var productoIds = g.Select(x => x.ProductoId).Distinct().ToArray();
                if (productoIds.Length != 1)
                {
                    throw new BusinessRuleException(
                        "Una misma existencia no puede asociarse a más de un ProductoId en la misma operación.");
                }

                return new InventarioDemandaExistencia(
                    productoIds[0],
                    g.Key.ProductoVarianteId,
                    g.Key.AlmacenId,
                    g.Key.UbicacionAlmacenId,
                    g.Sum(x => x.Cantidad));
            })
            .OrderBy(d => d.ProductoVarianteId)
            .ThenBy(d => d.AlmacenId)
            .ThenBy(d => d.UbicacionAlmacenId ?? 0)
            .ToList();

        if (consolidada.Count == 0)
        {
            return new InventarioExistenciaLockSet(
                new Dictionary<InventarioExistenciaClave, ExistenciaVariante>(),
                Array.Empty<InventarioDemandaExistencia>());
        }

        var existencias = new Dictionary<InventarioExistenciaClave, ExistenciaVariante>();

        foreach (var demanda in consolidada)
        {
            var existencia = incluirEliminados
                ? await _repository.GetByClaveParaReversionAsync(
                    demanda.ProductoVarianteId,
                    demanda.AlmacenId,
                    demanda.UbicacionAlmacenId)
                : await _repository.GetByClaveAsync(
                    demanda.ProductoVarianteId,
                    demanda.AlmacenId,
                    demanda.UbicacionAlmacenId,
                    forUpdate: true);

            if (existencia is null)
            {
                throw new BusinessRuleException(
                    $"No existe stock autoritativo para variante {demanda.ProductoVarianteId}, almacén {demanda.AlmacenId} y ubicación {(demanda.UbicacionAlmacenId?.ToString() ?? "raíz")}.");
            }

            if (existencia.ProductoVariante.ProductoId != demanda.ProductoId)
            {
                throw new BusinessRuleException(
                    $"La variante ID '{demanda.ProductoVarianteId}' no pertenece al producto ID '{demanda.ProductoId}'.");
            }

            if (esDeduccion && existencia.StockDisponible < demanda.Cantidad)
            {
                throw new BusinessRuleException(
                    $"Stock insuficiente en la existencia de variante '{existencia.ProductoVariante.Sku}': disponible {existencia.StockDisponible}, solicitado {demanda.Cantidad}.");
            }

            existencias.Add(demanda.Clave, existencia);
        }

        return new InventarioExistenciaLockSet(existencias, consolidada);
    }

    public async Task AjustarStockFisicoPesimistaAsync(
        InventarioExistenciaClave clave,
        int cantidadActualEsperada,
        int cantidadNueva)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("AjustarStockFisicoPesimistaAsync requiere una transacción activa.");

        if (clave.ProductoVarianteId <= 0 || clave.AlmacenId <= 0 ||
            (clave.UbicacionAlmacenId.HasValue && clave.UbicacionAlmacenId.Value <= 0))
        {
            throw new BusinessRuleException("La clave de existencia no es válida.");
        }

        if (cantidadActualEsperada < 0 || cantidadNueva < 0)
            throw new BusinessRuleException("Las cantidades de inventario no pueden ser negativas.");

        var existencia = await _repository.GetByClaveAsync(
            clave.ProductoVarianteId,
            clave.AlmacenId,
            clave.UbicacionAlmacenId,
            forUpdate: true)
            ?? throw new BusinessRuleException("La existencia indicada no existe.");

        if (existencia.StockFisico != cantidadActualEsperada)
        {
            throw new BusinessRuleException(
                "El inventario cambió desde que se cargó el formulario. Actualiza los datos e inténtalo nuevamente.");
        }

        try
        {
            existencia.EstablecerStocks(
                cantidadNueva,
                existencia.StockReservado,
                existencia.StockTransito,
                existencia.StockMinimo,
                existencia.StockMaximo);
        }
        catch (ArgumentException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }

        _repository.Update(existencia);
    }
}
