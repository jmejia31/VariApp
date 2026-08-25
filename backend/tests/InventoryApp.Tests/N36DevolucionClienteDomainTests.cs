using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Xunit;

namespace InventoryApp.Tests;

public class N36DevolucionClienteDomainTests
{
    [Fact]
    public void ValidarDocumento_requiere_venta_motivo_y_detalle()
    {
        var devolucion = CrearValida();
        devolucion.VentaId = 0;

        Assert.Throws<InvalidOperationException>(() => devolucion.ValidarDocumento());

        devolucion = CrearValida();
        devolucion.Motivo = " ";
        Assert.Throws<InvalidOperationException>(() => devolucion.ValidarDocumento());

        devolucion = CrearValida();
        devolucion.Detalles.Clear();
        Assert.Throws<InvalidOperationException>(() => devolucion.ValidarDocumento());
    }

    [Fact]
    public void ValidarDocumento_rechaza_detalle_venta_duplicado()
    {
        var devolucion = CrearValida();
        devolucion.Detalles.Add(CrearDetalle(10));

        Assert.Throws<InvalidOperationException>(() => devolucion.ValidarDocumento());
    }

    [Fact]
    public void Confirmar_y_anular_respetan_lifecycle_y_utc()
    {
        var devolucion = CrearValida();
        var confirmadaUtc = new DateTime(2026, 8, 25, 18, 0, 0, DateTimeKind.Utc);
        var anuladaUtc = confirmadaUtc.AddMinutes(15);

        devolucion.Confirmar(7, confirmadaUtc);

        Assert.Equal(EstadoDevolucionCliente.Confirmada, devolucion.Estado);
        Assert.Equal(7, devolucion.ConfirmadaPorUsuarioId);
        Assert.Equal(confirmadaUtc, devolucion.FechaConfirmacionUtc);
        Assert.Throws<InvalidOperationException>(() => devolucion.Confirmar(7, confirmadaUtc));

        devolucion.Anular(9, "Error de captura", anuladaUtc);

        Assert.Equal(EstadoDevolucionCliente.Anulada, devolucion.Estado);
        Assert.Equal(9, devolucion.AnuladaPorUsuarioId);
        Assert.Equal("Error de captura", devolucion.MotivoAnulacion);
        Assert.Equal(anuladaUtc, devolucion.FechaAnulacionUtc);
    }

    [Fact]
    public void Confirmar_rechaza_fecha_no_utc()
    {
        var devolucion = CrearValida();
        var local = DateTime.SpecifyKind(new DateTime(2026, 8, 25, 12, 0, 0), DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() => devolucion.Confirmar(1, local));
    }

    [Fact]
    public void TotalReferencial_suma_detalles_sin_inventar_credito_financiero()
    {
        var devolucion = CrearValida();
        devolucion.Detalles.Add(new DevolucionClienteDetalle
        {
            VentaDetalleId = 11,
            ProductoId = 2,
            Cantidad = 1,
            PrecioUnitarioSnapshot = 50.125m,
            ProductoNombreSnapshot = "Producto B"
        });

        Assert.Equal(250.3750m, devolucion.TotalReferencial);
    }

    private static DevolucionCliente CrearValida() => new()
    {
        NumeroDevolucion = "DEV-CLI-0001",
        VentaId = 100,
        ClienteId = 5,
        ClienteNombreSnapshot = "Cliente prueba",
        Motivo = "Producto no requerido",
        Detalles = new List<DevolucionClienteDetalle> { CrearDetalle(10) }
    };

    private static DevolucionClienteDetalle CrearDetalle(int ventaDetalleId) => new()
    {
        VentaDetalleId = ventaDetalleId,
        ProductoId = 1,
        ProductoVarianteId = 2,
        Cantidad = 2,
        PrecioUnitarioSnapshot = 100.125m,
        ProductoNombreSnapshot = "Producto A",
        ProductoSkuSnapshot = "SKU-A"
    };
}
