using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

public static class TransferenciaInventarioExistenciaContext
{
    public static IReadOnlyList<InventarioDemandaExistencia> ConstruirDemandasDespacho(
        TransferenciaInventario transferencia) =>
        ConstruirDemandas(transferencia, destino: false, usarCantidadRecibida: false);

    public static IReadOnlyList<InventarioDemandaExistencia> ConstruirDemandasTransitoDestino(
        TransferenciaInventario transferencia) =>
        ConstruirDemandas(transferencia, destino: true, usarCantidadRecibida: false);

    public static IReadOnlyList<InventarioDemandaExistencia> ConstruirDemandasBloqueoDespacho(
        TransferenciaInventario transferencia) =>
        ConstruirDemandasDespacho(transferencia)
            .Concat(ConstruirDemandasTransitoDestino(transferencia))
            .ToList();

    public static IReadOnlyList<InventarioDemandaExistencia> ConstruirDemandasRecepcionDestino(
        TransferenciaInventario transferencia) =>
        ConstruirDemandas(transferencia, destino: true, usarCantidadRecibida: true);

    private static IReadOnlyList<InventarioDemandaExistencia> ConstruirDemandas(
        TransferenciaInventario transferencia,
        bool destino,
        bool usarCantidadRecibida)
    {
        ArgumentNullException.ThrowIfNull(transferencia);
        var almacenId = destino ? transferencia.AlmacenDestinoId : transferencia.AlmacenOrigenId;
        if (almacenId <= 0)
            throw new BusinessRuleException($"La transferencia no tiene un almacén de {(destino ? "destino" : "origen")} válido.");
        if (transferencia.Detalles.Count == 0)
            throw new BusinessRuleException("La transferencia no contiene detalles físicos.");

        var demandas = new List<InventarioDemandaExistencia>(transferencia.Detalles.Count);
        foreach (var detalle in transferencia.Detalles)
        {
            if (detalle.ProductoVarianteId <= 0 || detalle.ProductoVariante is null || detalle.ProductoVariante.ProductoId <= 0)
                throw new BusinessRuleException("Cada detalle debe tener una variante cargada con producto válido.");

            var cantidad = usarCantidadRecibida ? detalle.CantidadRecibida : detalle.CantidadDespachada;
            if (cantidad <= 0)
            {
                throw new BusinessRuleException(
                    usarCantidadRecibida
                        ? "Cada detalle debe registrar una cantidad recibida mayor que cero para materializar stock destino."
                        : "Cada detalle debe tener una cantidad despachada mayor que cero.");
            }

            demandas.Add(new InventarioDemandaExistencia(
                detalle.ProductoVariante.ProductoId,
                detalle.ProductoVarianteId,
                almacenId,
                destino ? detalle.UbicacionDestinoId : detalle.UbicacionOrigenId,
                cantidad));
        }

        return demandas;
    }
}
