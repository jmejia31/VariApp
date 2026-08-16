using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

public static class TransferenciaInventarioExistenciaContext
{
    public static IReadOnlyList<InventarioDemandaExistencia> ConstruirDemandasDespacho(
        TransferenciaInventario transferencia)
    {
        ArgumentNullException.ThrowIfNull(transferencia);
        if (transferencia.AlmacenOrigenId <= 0)
            throw new BusinessRuleException("La transferencia no tiene un almacén de origen válido.");
        if (transferencia.Detalles.Count == 0)
            throw new BusinessRuleException("La transferencia no contiene detalles para despachar.");

        var demandas = new List<InventarioDemandaExistencia>(transferencia.Detalles.Count);
        foreach (var detalle in transferencia.Detalles)
        {
            if (detalle.ProductoVarianteId <= 0 || detalle.ProductoVariante is null || detalle.ProductoVariante.ProductoId <= 0)
                throw new BusinessRuleException("Cada detalle debe tener una variante cargada con producto válido antes del despacho.");
            if (detalle.CantidadDespachada <= 0)
                throw new BusinessRuleException("Cada detalle debe tener una cantidad despachada mayor que cero.");

            demandas.Add(new InventarioDemandaExistencia(
                detalle.ProductoVariante.ProductoId,
                detalle.ProductoVarianteId,
                transferencia.AlmacenOrigenId,
                detalle.UbicacionOrigenId,
                detalle.CantidadDespachada));
        }

        return demandas;
    }
}
