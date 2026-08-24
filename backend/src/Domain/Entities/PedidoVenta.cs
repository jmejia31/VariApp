using InventoryApp.Domain.Common;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

/// <summary>
/// Documento comercial de pedido. N3.2 lo mantiene independiente de la Venta legacy
/// y sin efectos de inventario, Kardex, facturación o finanzas.
/// </summary>
public class PedidoVenta : AuditableEntity
{
    public int? CotizacionId { get; private set; }
    public Cotizacion? Cotizacion { get; private set; }

    public int ClienteId { get; private set; }
    public Cliente Cliente { get; private set; } = null!;
    public string ClienteNombreSnapshot { get; private set; } = string.Empty;
    public string? ClienteDocumentoSnapshot { get; private set; }
    public string? Observaciones { get; private set; }

    public ICollection<PedidoVentaDetalle> Detalles { get; private set; } = new List<PedidoVentaDetalle>();

    public decimal Total => Detalles.Sum(x => x.Total);

    /// <summary>
    /// Materializa el pedido a partir de una cotización aceptada y persistida.
    /// No convierte ni muta la cotización; esa coordinación transaccional pertenece
    /// a Application (N3.2.D), donde también deberá resolverse la cardinalidad/idempotencia.
    /// </summary>
    public static PedidoVenta CrearDesdeCotizacion(Cotizacion cotizacion)
    {
        ArgumentNullException.ThrowIfNull(cotizacion);

        if (cotizacion.Id <= 0)
            throw new InvalidOperationException("La cotización de origen debe estar persistida.");
        if (cotizacion.Estado != EstadoCotizacion.Aceptada)
            throw new InvalidOperationException("Solo una cotización aceptada puede originar un pedido.");

        cotizacion.ValidarDocumento();

        var pedido = new PedidoVenta
        {
            CotizacionId = cotizacion.Id,
            Cotizacion = cotizacion,
            ClienteId = cotizacion.ClienteId,
            Cliente = cotizacion.Cliente,
            ClienteNombreSnapshot = cotizacion.ClienteNombreSnapshot,
            ClienteDocumentoSnapshot = cotizacion.ClienteDocumentoSnapshot,
            Observaciones = cotizacion.Observaciones
        };

        foreach (var detalle in cotizacion.Detalles)
            pedido.Detalles.Add(PedidoVentaDetalle.CrearDesdeCotizacion(detalle));

        pedido.ValidarDocumento();
        return pedido;
    }

    public void ActualizarObservaciones(string? observaciones)
    {
        Observaciones = string.IsNullOrWhiteSpace(observaciones)
            ? null
            : observaciones.Trim();
    }

    public void ValidarDocumento()
    {
        if (ClienteId <= 0)
            throw new InvalidOperationException("El cliente es obligatorio.");
        if (string.IsNullOrWhiteSpace(ClienteNombreSnapshot))
            throw new InvalidOperationException("El snapshot del cliente es obligatorio.");
        if (CotizacionId is <= 0)
            throw new InvalidOperationException("La cotización de origen debe ser válida cuando se especifica.");
        if (Detalles.Count == 0)
            throw new InvalidOperationException("El pedido debe contener al menos un detalle.");

        foreach (var detalle in Detalles)
            detalle.Validar();
    }
}

public class PedidoVentaDetalle : AuditableEntity
{
    public int PedidoVentaId { get; private set; }
    public PedidoVenta PedidoVenta { get; private set; } = null!;

    public int ProductoId { get; private set; }
    public Producto Producto { get; private set; } = null!;

    public int? ProductoVarianteId { get; private set; }
    public ProductoVariante? ProductoVariante { get; private set; }

    public string? ProductoSkuSnapshot { get; private set; }
    public string? ProductoNombreSnapshot { get; private set; }
    public string? ProductoMarcaSnapshot { get; private set; }
    public string? ProductoModeloSnapshot { get; private set; }
    public string? ProductoColorSnapshot { get; private set; }
    public string? ProductoTallaSnapshot { get; private set; }

    public decimal Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public decimal Total => Cantidad * PrecioUnitario;

    internal static PedidoVentaDetalle CrearDesdeCotizacion(CotizacionDetalle detalle)
    {
        ArgumentNullException.ThrowIfNull(detalle);
        detalle.Validar();

        return new PedidoVentaDetalle
        {
            ProductoId = detalle.ProductoId,
            Producto = detalle.Producto,
            ProductoVarianteId = detalle.ProductoVarianteId,
            ProductoVariante = detalle.ProductoVariante,
            ProductoSkuSnapshot = detalle.ProductoSkuSnapshot,
            ProductoNombreSnapshot = detalle.ProductoNombreSnapshot,
            ProductoMarcaSnapshot = detalle.ProductoMarcaSnapshot,
            ProductoModeloSnapshot = detalle.ProductoModeloSnapshot,
            ProductoColorSnapshot = detalle.ProductoColorSnapshot,
            ProductoTallaSnapshot = detalle.ProductoTallaSnapshot,
            Cantidad = detalle.Cantidad,
            PrecioUnitario = detalle.PrecioUnitario
        };
    }

    public void Validar()
    {
        if (ProductoId <= 0)
            throw new InvalidOperationException("El producto es obligatorio.");
        if (ProductoVarianteId is <= 0)
            throw new InvalidOperationException("La variante debe ser válida cuando se especifica.");
        if (Cantidad <= 0)
            throw new InvalidOperationException("La cantidad debe ser mayor que cero.");
        if (PrecioUnitario < 0)
            throw new InvalidOperationException("El precio unitario no puede ser negativo.");
    }
}