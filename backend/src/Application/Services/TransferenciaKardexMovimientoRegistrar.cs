using InventoryApp.Application.Common;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Common;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

/// <summary>
/// Emite Kardex físico correlacionado para transferencias usando exclusivamente
/// el writer canónico de N1.5 y el origen relacional TransferenciaInventarioId.
/// </summary>
public sealed class TransferenciaKardexMovimientoRegistrar
{
    private readonly IKardexMovimientoWriter _writer;

    public TransferenciaKardexMovimientoRegistrar(IKardexMovimientoWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public async Task RegistrarDespachoAsync(
        TransferenciaInventario transferencia,
        IReadOnlyCollection<TransferenciaInventarioTransitoTransicion> transiciones,
        int usuarioId,
        string? nombreUsuario)
    {
        ValidarTransferenciaPersistida(transferencia, usuarioId);
        var correlationId = KardexCorrelationId.TransferenciaDespachar(transferencia.Id);
        var origen = OrigenMovimientoInventario.DesdeTransferenciaInventario(transferencia.Id);

        foreach (var transicion in transiciones.Where(x => x.CantidadFisica < 0))
        {
            var detalle = BuscarDetalle(transferencia, transicion.Clave, origen: true);
            var cantidad = checked(-transicion.CantidadFisica);
            var movimiento = CrearMovimiento(
                transferencia,
                detalle,
                transicion,
                TipoMovimientoInventario.Salida,
                CausaMovimientoInventario.TransferenciaDespacho,
                cantidad,
                usuarioId,
                nombreUsuario,
                $"Despacho de transferencia {transferencia.Numero} hacia almacén {transferencia.AlmacenDestinoId}.");
            var contexto = ContextoFisicoMovimientoInventario.Crear(
                transicion.Clave.ProductoVarianteId,
                transicion.Clave.AlmacenId,
                transicion.Clave.UbicacionAlmacenId,
                correlationId);

            await _writer.RegistrarFisicoAsync(movimiento, origen, contexto);
        }
    }

    public async Task RegistrarRecepcionAsync(
        TransferenciaInventario transferencia,
        IReadOnlyCollection<TransferenciaInventarioTransitoTransicion> transiciones,
        int usuarioId,
        string? nombreUsuario)
    {
        ValidarTransferenciaPersistida(transferencia, usuarioId);
        var correlationId = KardexCorrelationId.TransferenciaRecibir(transferencia.Id);
        var origen = OrigenMovimientoInventario.DesdeTransferenciaInventario(transferencia.Id);

        foreach (var transicion in transiciones.Where(x => x.CantidadFisica > 0))
        {
            var detalle = BuscarDetalle(transferencia, transicion.Clave, origen: false);
            var movimiento = CrearMovimiento(
                transferencia,
                detalle,
                transicion,
                TipoMovimientoInventario.Entrada,
                CausaMovimientoInventario.TransferenciaRecepcion,
                transicion.CantidadFisica,
                usuarioId,
                nombreUsuario,
                $"Recepción de transferencia {transferencia.Numero} desde almacén {transferencia.AlmacenOrigenId}.");
            var contexto = ContextoFisicoMovimientoInventario.Crear(
                transicion.Clave.ProductoVarianteId,
                transicion.Clave.AlmacenId,
                transicion.Clave.UbicacionAlmacenId,
                correlationId);

            await _writer.RegistrarFisicoAsync(movimiento, origen, contexto);
        }
    }

    private static MovimientoInventario CrearMovimiento(
        TransferenciaInventario transferencia,
        TransferenciaInventarioDetalle detalle,
        TransferenciaInventarioTransitoTransicion transicion,
        TipoMovimientoInventario tipo,
        CausaMovimientoInventario causa,
        int cantidad,
        int usuarioId,
        string? nombreUsuario,
        string descripcion) =>
        new()
        {
            ProductoId = detalle.ProductoVariante.ProductoId,
            ProductoVarianteId = detalle.ProductoVarianteId,
            ProductoMarcaSnapshot = detalle.ProductoMarcaSnapshot,
            ProductoModeloSnapshot = detalle.ProductoModeloSnapshot,
            ProductoColorSnapshot = detalle.ProductoColorSnapshot,
            ProductoTallaSnapshot = detalle.ProductoTallaSnapshot,
            ProductoSkuSnapshot = detalle.ProductoSkuSnapshot,
            Tipo = tipo,
            Causa = causa,
            Cantidad = cantidad,
            StockAnterior = transicion.StockFisicoAnterior,
            StockNuevo = transicion.StockFisicoNuevo,
            CostoUnitario = detalle.ProductoVariante.Costo,
            Descripcion = descripcion,
            CreadoPorUsuarioId = usuarioId,
            CreadoPorNombreUsuario = nombreUsuario,
            Fecha = DateTime.UtcNow
        };

    private static TransferenciaInventarioDetalle BuscarDetalle(
        TransferenciaInventario transferencia,
        InventarioExistenciaClave clave,
        bool origen)
    {
        var detalle = transferencia.Detalles.FirstOrDefault(d =>
            d.ProductoVarianteId == clave.ProductoVarianteId &&
            (origen ? d.UbicacionOrigenId : d.UbicacionDestinoId) == clave.UbicacionAlmacenId);
        return detalle ?? throw new BusinessRuleException(
            "No se encontró el detalle de transferencia correspondiente a la transición física de Kardex.");
    }

    private static void ValidarTransferenciaPersistida(TransferenciaInventario transferencia, int usuarioId)
    {
        ArgumentNullException.ThrowIfNull(transferencia);
        if (transferencia.Id <= 0)
            throw new BusinessRuleException("La transferencia debe estar persistida antes de registrar Kardex.");
        if (usuarioId <= 0)
            throw new BusinessRuleException("El usuario de Kardex debe ser válido.");
    }
}
