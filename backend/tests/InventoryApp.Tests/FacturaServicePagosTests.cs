using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;
using CatalogoMetodoPago = InventoryApp.Domain.Entities.Catalogos.MetodoPago;

namespace InventoryApp.Tests;

public class FacturaServicePagosTests
{
    [Fact]
    public async Task RegistrarPagoParcial_ActualizaSaldoYEstado()
    {
        var factura = CrearFactura(300m);
        var (service, repository) = CrearServicio(factura);

        var resultado = await service.RegistrarPagoAsync(
            factura.Id,
            new RegistrarFacturaPagoDto { Monto = 100m, MetodoPago = "Transferencia" },
            7,
            "tester");

        Assert.Equal(100m, resultado.TotalPagado);
        Assert.Equal(200m, resultado.SaldoPendiente);
        Assert.Equal("ParcialmentePagada", resultado.Estado);
        Assert.Equal(EstadoPago.Parcial, factura.Venta!.EstadoPago);
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegistrarPagoCompleto_MarcaFacturaPagada()
    {
        var factura = CrearFactura(300m);
        var (service, _) = CrearServicio(factura);

        var resultado = await service.RegistrarPagoAsync(
            factura.Id,
            new RegistrarFacturaPagoDto { Monto = 300m, MetodoPago = "Efectivo" },
            7,
            "tester");

        Assert.Equal(300m, resultado.TotalPagado);
        Assert.Equal(0m, resultado.SaldoPendiente);
        Assert.Equal("Pagada", resultado.Estado);
        Assert.Equal(EstadoPago.Pagado, factura.Venta!.EstadoPago);
    }

    [Fact]
    public async Task RegistrarPagoSuperiorAlSaldo_EsRechazado()
    {
        var factura = CrearFactura(300m);
        var (service, _) = CrearServicio(factura);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RegistrarPagoAsync(
                factura.Id,
                new RegistrarFacturaPagoDto { Monto = 300.01m, MetodoPago = "Efectivo" },
                7,
                "tester"));

        Assert.Contains("saldo pendiente", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factura.Pagos);
    }

    [Fact]
    public async Task AnularPago_RecalculaSaldoYConservaTrazabilidad()
    {
        var factura = CrearFactura(300m);
        factura.Pagos.Add(new FacturaPago
        {
            Id = 22,
            FacturaId = factura.Id,
            Monto = 300m,
            MetodoPago = MetodoPago.Efectivo
        });
        factura.TotalPagado = 300m;
        factura.SaldoPendiente = 0m;
        factura.Estado = EstadoFactura.Pagada;

        var (service, _) = CrearServicio(factura);
        var resultado = await service.AnularPagoAsync(
            factura.Id,
            22,
            new AnularFacturaPagoDto { Motivo = "Pago registrado por error" },
            8,
            "supervisor");

        Assert.Equal(0m, resultado.TotalPagado);
        Assert.Equal(300m, resultado.SaldoPendiente);
        Assert.Equal("Emitida", resultado.Estado);
        Assert.True(factura.Pagos.Single().Anulado);
        Assert.Equal("Pago registrado por error", factura.Pagos.Single().MotivoAnulacion);
        Assert.Equal(8, factura.Pagos.Single().AnuladoPorUsuarioId);
    }

    private static Factura CrearFactura(decimal total) => new()
    {
        Id = 15,
        VentaId = 9,
        NumeroFactura = "FAC-000015",
        Estado = EstadoFactura.Emitida,
        EmpresaNombre = "VariStorehn",
        ClienteNombre = "Cliente prueba",
        VendedorUsuarioId = 7,
        VendedorNombreUsuario = "tester",
        Total = total,
        SaldoPendiente = total,
        Venta = new Venta
        {
            Id = 9,
            NumeroVenta = "VEN-000009",
            EstadoPago = EstadoPago.Pendiente,
            MetodoPagoId = 1,
            MetodoPagoCatalogo = CrearMetodoPago(1, "EFECTIVO", "Efectivo")
        }
    };

    private static (FacturaService Service, Mock<IFacturaRepository> Repository) CrearServicio(Factura factura)
    {
        var repository = new Mock<IFacturaRepository>();
        repository.Setup(x => x.GetByIdAsync(factura.Id)).ReturnsAsync(factura);
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        repository.Setup(x => x.GetMetodoPagoPorCodigoONombreAsync(It.IsAny<string>()))
            .ReturnsAsync((string valor) => valor.Trim().Equals("Transferencia", StringComparison.OrdinalIgnoreCase)
                ? CrearMetodoPago(2, "TRANSFERENCIA", "Transferencia")
                : valor.Trim().Equals("Efectivo", StringComparison.OrdinalIgnoreCase)
                    ? CrearMetodoPago(1, "EFECTIVO", "Efectivo")
                    : null);

        var empresa = new Mock<IEmpresaConfiguracionService>();
        empresa.Setup(x => x.GetActivaAsync()).ReturnsAsync(new EmpresaConfiguracionDto());

        return (new FacturaService(repository.Object, empresa.Object), repository);
    }

    private static CatalogoMetodoPago CrearMetodoPago(int id, string codigo, string nombre) => new()
    {
        Id = id,
        Codigo = codigo,
        Nombre = nombre,
        Activo = true
    };
}
