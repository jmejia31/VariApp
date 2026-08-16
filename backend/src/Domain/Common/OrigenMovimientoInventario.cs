using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Common;

public sealed record OrigenMovimientoInventario
{
    private OrigenMovimientoInventario(
        TipoOrigenMovimientoInventario tipo,
        int? compraId,
        int? ventaId,
        int? consumoInsumoId,
        int? ajusteInventarioId,
        int? transferenciaInventarioId)
    {
        Tipo = tipo;
        CompraId = compraId;
        VentaId = ventaId;
        ConsumoInsumoId = consumoInsumoId;
        AjusteInventarioId = ajusteInventarioId;
        TransferenciaInventarioId = transferenciaInventarioId;
    }

    public TipoOrigenMovimientoInventario Tipo { get; }
    public int? CompraId { get; }
    public int? VentaId { get; }
    public int? ConsumoInsumoId { get; }
    public int? AjusteInventarioId { get; }
    public int? TransferenciaInventarioId { get; }

    public int Id => Tipo switch
    {
        TipoOrigenMovimientoInventario.Compra => CompraId!.Value,
        TipoOrigenMovimientoInventario.Venta => VentaId!.Value,
        TipoOrigenMovimientoInventario.ConsumoInsumo => ConsumoInsumoId!.Value,
        TipoOrigenMovimientoInventario.AjusteInventario => AjusteInventarioId!.Value,
        TipoOrigenMovimientoInventario.TransferenciaInventario => TransferenciaInventarioId!.Value,
        _ => throw new InvalidOperationException($"Origen tipado no soportado: {Tipo}.")
    };

    public static OrigenMovimientoInventario DesdeCompra(int compraId)
        => Crear(compraId, null, null, null, null);

    public static OrigenMovimientoInventario DesdeVenta(int ventaId)
        => Crear(null, ventaId, null, null, null);

    public static OrigenMovimientoInventario DesdeConsumoInsumo(int consumoInsumoId)
        => Crear(null, null, consumoInsumoId, null, null);

    public static OrigenMovimientoInventario DesdeAjusteInventario(int ajusteInventarioId)
        => Crear(null, null, null, ajusteInventarioId, null);

    public static OrigenMovimientoInventario DesdeTransferenciaInventario(int transferenciaInventarioId)
        => Crear(null, null, null, null, transferenciaInventarioId);

    public static OrigenMovimientoInventario DesdeIds(
        int? compraId,
        int? ventaId,
        int? consumoInsumoId,
        int? ajusteInventarioId,
        int? transferenciaInventarioId = null)
        => Crear(compraId, ventaId, consumoInsumoId, ajusteInventarioId, transferenciaInventarioId);

    private static OrigenMovimientoInventario Crear(
        int? compraId,
        int? ventaId,
        int? consumoInsumoId,
        int? ajusteInventarioId,
        int? transferenciaInventarioId)
    {
        var ids = new[]
        {
            compraId,
            ventaId,
            consumoInsumoId,
            ajusteInventarioId,
            transferenciaInventarioId
        };

        if (ids.Count(id => id.HasValue) != 1)
            throw new ArgumentException("El movimiento debe tener exactamente un origen tipado.");

        if (ids.Any(id => id.HasValue && id.Value <= 0))
            throw new ArgumentOutOfRangeException(nameof(compraId), "Los identificadores de origen deben ser mayores que cero.");

        if (compraId.HasValue)
            return new(TipoOrigenMovimientoInventario.Compra, compraId, null, null, null, null);
        if (ventaId.HasValue)
            return new(TipoOrigenMovimientoInventario.Venta, null, ventaId, null, null, null);
        if (consumoInsumoId.HasValue)
            return new(TipoOrigenMovimientoInventario.ConsumoInsumo, null, null, consumoInsumoId, null, null);
        if (ajusteInventarioId.HasValue)
            return new(TipoOrigenMovimientoInventario.AjusteInventario, null, null, null, ajusteInventarioId, null);

        return new(
            TipoOrigenMovimientoInventario.TransferenciaInventario,
            null,
            null,
            null,
            null,
            transferenciaInventarioId);
    }
}
