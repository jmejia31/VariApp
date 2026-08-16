using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

public sealed record TransferenciaInventarioExistenciaTransicion(
    InventarioExistenciaClave Clave,
    int StockAnterior,
    int StockNuevo,
    int Cantidad);

/// <summary>
/// Orquesta la deducción autoritativa del almacén origen al despachar una
/// transferencia. Opera únicamente sobre ExistenciaVariante bajo locks físicos.
/// </summary>
public sealed class TransferenciaInventarioExistenciaService
{
    private readonly IExistenciaVarianteConcurrencyService _existencias;

    public TransferenciaInventarioExistenciaService(IExistenciaVarianteConcurrencyService existencias)
    {
        _existencias = existencias;
    }

    public Task<InventarioExistenciaLockSet> BloquearParaDespachoAsync(TransferenciaInventario transferencia)
    {
        var demandas = TransferenciaInventarioExistenciaContext.ConstruirDemandasDespacho(transferencia);
        return _existencias.BloquearYValidarExistenciasAsync(demandas, esDeduccion: true);
    }

    public async Task<IReadOnlyList<TransferenciaInventarioExistenciaTransicion>> AplicarDespachoAsync(
        InventarioExistenciaLockSet lockSet)
    {
        ArgumentNullException.ThrowIfNull(lockSet);
        if (lockSet.Demandas.Count == 0)
            throw new BusinessRuleException("No existen demandas físicas bloqueadas para el despacho.");

        var transiciones = new List<TransferenciaInventarioExistenciaTransicion>(lockSet.Demandas.Count);
        foreach (var demanda in lockSet.Demandas)
        {
            if (!lockSet.Existencias.TryGetValue(demanda.Clave, out var existencia))
                throw new BusinessRuleException("No se encontró la existencia física bloqueada para el despacho.");

            var stockAnterior = existencia.StockFisico;
            var stockNuevo = checked(stockAnterior - demanda.Cantidad);
            if (stockNuevo < 0 || stockNuevo < existencia.StockReservado)
                throw new BusinessRuleException("El despacho dejaría el stock físico por debajo de la reserva vigente.");

            await _existencias.AjustarStockFisicoPesimistaAsync(
                demanda.Clave,
                stockAnterior,
                stockNuevo);

            transiciones.Add(new TransferenciaInventarioExistenciaTransicion(
                demanda.Clave,
                stockAnterior,
                stockNuevo,
                demanda.Cantidad));
        }

        return transiciones;
    }
}
