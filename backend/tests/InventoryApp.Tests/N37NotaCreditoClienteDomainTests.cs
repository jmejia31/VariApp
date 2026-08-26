using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Xunit;

namespace InventoryApp.Tests;

public class N37NotaCreditoClienteDomainTests
{
    private static readonly DateTime FechaUtc = new(2026, 8, 26, 13, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CrearDesdeFactura_Elegible_PreservaAutoridadFacturaYVenta()
    {
        var factura = CrearFactura(EstadoFactura.Emitida, total: 1000m);

        var nota = NotaCreditoCliente.CrearDesdeFactura(factura, 250m, "Ajuste comercial", " observación ");

        Assert.Equal(factura.Id, nota.FacturaId);
        Assert.Same(factura, nota.Factura);
        Assert.Equal(factura.VentaId, nota.VentaId);
        Assert.Equal("HNL", nota.Moneda);
        Assert.Equal(250m, nota.MontoCredito);
        Assert.Equal("Ajuste comercial", nota.Motivo);
        Assert.Equal("observación", nota.Observaciones);
        Assert.Equal(EstadoNotaCreditoCliente.Borrador, nota.Estado);
        Assert.True(nota.EsEditable);
    }

    [Theory]
    [InlineData(EstadoFactura.Borrador)]
    [InlineData(EstadoFactura.Anulada)]
    [InlineData(EstadoFactura.Cancelada)]
    public void CrearDesdeFactura_NoElegible_FallaCerrado(EstadoFactura estado)
    {
        var factura = CrearFactura(estado, 1000m);
        Assert.Throws<InvalidOperationException>(() =>
            NotaCreditoCliente.CrearDesdeFactura(factura, 100m, "motivo"));
    }

    [Fact]
    public void CrearDesdeFactura_MontoMayorQueFactura_FallaCerrado()
    {
        var factura = CrearFactura(EstadoFactura.Pagada, 500m);
        Assert.Throws<InvalidOperationException>(() =>
            NotaCreditoCliente.CrearDesdeFactura(factura, 500.01m, "motivo"));
    }

    [Fact]
    public void Emitir_Y_Anular_RespetaLifecycleSinMutarFactura()
    {
        var factura = CrearFactura(EstadoFactura.Emitida, 1000m);
        var nota = NotaCreditoCliente.CrearDesdeFactura(factura, 100m, "motivo");

        nota.Emitir(FechaUtc);

        Assert.Equal(EstadoNotaCreditoCliente.Emitida, nota.Estado);
        Assert.False(nota.EsEditable);
        Assert.Equal(FechaUtc, nota.FechaEmisionUtc);
        Assert.Equal(EstadoFactura.Emitida, factura.Estado);
        Assert.Equal(0m, factura.TotalPagado);

        var anulacionUtc = FechaUtc.AddMinutes(5);
        nota.Anular("corrección", anulacionUtc);

        Assert.Equal(EstadoNotaCreditoCliente.Anulada, nota.Estado);
        Assert.Equal(anulacionUtc, nota.FechaAnulacionUtc);
        Assert.Equal("corrección", nota.MotivoAnulacion);
        Assert.Equal(EstadoFactura.Emitida, factura.Estado);
    }

    [Fact]
    public void Actualizar_DespuesDeEmitir_FallaCerrado()
    {
        var factura = CrearFactura(EstadoFactura.Emitida, 1000m);
        var nota = NotaCreditoCliente.CrearDesdeFactura(factura, 100m, "motivo");
        nota.Emitir(FechaUtc);

        Assert.Throws<InvalidOperationException>(() => nota.Actualizar(90m, "nuevo", null));
    }

    [Fact]
    public void Lifecycle_RequiereFechasUtc()
    {
        var factura = CrearFactura(EstadoFactura.Emitida, 1000m);
        var nota = NotaCreditoCliente.CrearDesdeFactura(factura, 100m, "motivo");
        var local = DateTime.SpecifyKind(FechaUtc, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() => nota.Emitir(local));
    }

    private static Factura CrearFactura(EstadoFactura estado, decimal total) => new()
    {
        Id = 700,
        VentaId = 600,
        NumeroFactura = "F-700",
        Estado = estado,
        Moneda = "HNL",
        ClienteNombre = "Cliente",
        EmpresaNombre = "VariStore",
        Total = total,
        TotalPagado = 0m
    };
}
