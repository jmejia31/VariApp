using InventoryApp.Domain.Common;

namespace InventoryApp.Domain.Entities;

/// <summary>
/// Capa contable FIFO. Es deliberadamente independiente de LoteInventario:
/// la identidad logística y la identidad de valoración resuelven problemas distintos.
/// </summary>
public sealed class CapaCostoInventario : AuditableEntity
{
    public int ProductoVarianteId { get; private set; }
    public int AlmacenId { get; private set; }
    public int? UbicacionAlmacenId { get; private set; }
    public int MovimientoInventarioOrigenId { get; private set; }
    public int CantidadOriginal { get; private set; }
    public int CantidadRestante { get; private set; }
    public decimal CostoUnitario { get; private set; }
    public DateTime FechaOrigenUtc { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;

    public bool Agotada => CantidadRestante == 0;
    public decimal ValorRestante => CantidadRestante * CostoUnitario;

    private CapaCostoInventario()
    {
    }

    public static CapaCostoInventario Crear(
        int productoVarianteId,
        int almacenId,
        int? ubicacionAlmacenId,
        int movimientoInventarioOrigenId,
        int cantidad,
        decimal costoUnitario,
        DateTime fechaOrigenUtc,
        string correlationId)
    {
        if (productoVarianteId <= 0)
            throw new ArgumentOutOfRangeException(nameof(productoVarianteId));
        if (almacenId <= 0)
            throw new ArgumentOutOfRangeException(nameof(almacenId));
        if (ubicacionAlmacenId.HasValue && ubicacionAlmacenId.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(ubicacionAlmacenId));
        if (movimientoInventarioOrigenId <= 0)
            throw new ArgumentOutOfRangeException(nameof(movimientoInventarioOrigenId));
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad de apertura debe ser mayor a cero.");
        if (costoUnitario < 0m)
            throw new ArgumentOutOfRangeException(nameof(costoUnitario), "El costo unitario no puede ser negativo.");
        if (fechaOrigenUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha de origen debe expresarse en UTC.", nameof(fechaOrigenUtc));
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("CorrelationId es obligatorio para una capa de costo.", nameof(correlationId));

        return new CapaCostoInventario
        {
            ProductoVarianteId = productoVarianteId,
            AlmacenId = almacenId,
            UbicacionAlmacenId = ubicacionAlmacenId,
            MovimientoInventarioOrigenId = movimientoInventarioOrigenId,
            CantidadOriginal = cantidad,
            CantidadRestante = cantidad,
            CostoUnitario = costoUnitario,
            FechaOrigenUtc = fechaOrigenUtc,
            CorrelationId = correlationId.Trim()
        };
    }

    public void Consumir(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad a consumir debe ser mayor a cero.");
        if (cantidad > CantidadRestante)
            throw new InvalidOperationException("La capa FIFO no tiene saldo suficiente para el consumo solicitado.");

        CantidadRestante -= cantidad;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void Restaurar(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad a restaurar debe ser mayor a cero.");
        if (CantidadRestante + cantidad > CantidadOriginal)
            throw new InvalidOperationException("La restauración no puede superar la cantidad original de la capa.");

        CantidadRestante += cantidad;
        FechaActualizacion = DateTime.UtcNow;
    }
}
