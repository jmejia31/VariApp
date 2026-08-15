using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

/// <summary>
/// Orquesta el cutover de AjusteInventario hacia ExistenciaVariante como autoridad
/// física de stock. Mantiene los locks y escrituras pesimistas fuera del bridge
/// legacy basado en ProductoVariante.Cantidad.
/// </summary>
public sealed class AjusteInventarioExistenciaCutoverService
{
    private readonly IExistenciaVarianteConcurrencyService _existenciaConcurrency;

    public AjusteInventarioExistenciaCutoverService(
        IExistenciaVarianteConcurrencyService existenciaConcurrency)
    {
        _existenciaConcurrency = existenciaConcurrency;
    }

    public Task<InventarioExistenciaLockSet> BloquearParaConfirmacionAsync(
        IEnumerable<AjusteInventarioDetalle> detalles)
    {
        var demandas = AjusteInventarioExistenciaStock.CrearDemandas(detalles);
        return _existenciaConcurrency.BloquearYValidarExistenciasAsync(
            demandas,
            esDeduccion: false);
    }

    public Task<InventarioExistenciaLockSet> BloquearParaReversionAsync(
        IEnumerable<AjusteInventarioDetalle> detalles)
    {
        var demandas = AjusteInventarioExistenciaStock.CrearDemandas(detalles);
        return _existenciaConcurrency.BloquearExistenciasParaReversionAsync(demandas);
    }

    public async Task<int> AplicarObjetivoConfirmacionAsync(
        InventarioExistenciaLockSet lockSet,
        AjusteInventarioDetalle detalle)
    {
        var existencia = AjusteInventarioExistenciaStock.ObtenerExistencia(lockSet, detalle);
        AjusteInventarioExistenciaStock.ValidarObjetivoContraReservado(
            existencia,
            detalle.CantidadObjetivo);

        var diferencia = AjusteInventarioExistenciaStock.CalcularDiferencia(
            existencia.StockFisico,
            detalle.CantidadObjetivo);
        if (diferencia == 0)
            throw new BusinessRuleException("El detalle no produce una diferencia real sobre el stock físico autoritativo.");

        var clave = AjusteInventarioExistenciaContext.CrearClave(detalle);
        await _existenciaConcurrency.AjustarStockFisicoPesimistaAsync(
            clave,
            existencia.StockFisico,
            detalle.CantidadObjetivo);

        return diferencia;
    }

    public async Task<int> AplicarReversionAsync(
        InventarioExistenciaLockSet lockSet,
        AjusteInventarioDetalle detalle)
    {
        var existencia = AjusteInventarioExistenciaStock.ObtenerExistencia(lockSet, detalle);
        var diferenciaOriginal = detalle.DiferenciaSnapshot
            ?? throw new BusinessRuleException("El ajuste no contiene una diferencia histórica válida para revertir.");
        var objetivo = AjusteInventarioExistenciaStock.CalcularObjetivoReversion(
            existencia,
            diferenciaOriginal);

        var clave = AjusteInventarioExistenciaContext.CrearClave(detalle);
        await _existenciaConcurrency.AjustarStockFisicoPesimistaAsync(
            clave,
            existencia.StockFisico,
            objetivo);

        return objetivo;
    }
}
