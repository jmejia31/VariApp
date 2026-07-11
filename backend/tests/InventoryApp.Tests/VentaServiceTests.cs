using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class VentaServiceTests
{
    private readonly Mock<IVentaRepository> _ventaRepoMock = new();
    private readonly Mock<IClienteRepository> _clienteRepoMock = new();
    private readonly Mock<IProductoRepository> _productoRepoMock = new();
    private readonly Mock<IFacturaRepository> _facturaRepoMock = new();
    private readonly Mock<IMovimientoInventarioRepository> _movInvRepoMock = new();
    private readonly Mock<IMovimientoFinancieroRepository> _movFinRepoMock = new();
    private readonly Mock<IEmpresaConfiguracionService> _empresaMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IAuditoriaService> _auditoriaMock = new();
    private readonly VentaService _service;

    public VentaServiceTests()
    {
        _currentUserMock.Setup(c => c.UsuarioId).Returns(4);
        _currentUserMock.Setup(c => c.NombreUsuario).Returns("vendedor1");
        _currentUserMock.Setup(c => c.NombreCompleto).Returns("Vendedor Uno");
        _ventaRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
        _empresaMock.Setup(e => e.GetActivaEntidadAsync()).ReturnsAsync(new EmpresaConfiguracion());

        _service = new VentaService(
            _ventaRepoMock.Object,
            _clienteRepoMock.Object,
            _productoRepoMock.Object,
            _facturaRepoMock.Object,
            _movInvRepoMock.Object,
            _movFinRepoMock.Object,
            _empresaMock.Object,
            _currentUserMock.Object,
            new FakeUnitOfWork(),
            _auditoriaMock.Object);
    }

    private static Producto ProductoDePrueba(int id = 1, int cantidad = 10) =>
        new() { Id = id, Nombre = "Mouse", Marca = "Logitech", Modelo = "M185", Cantidad = cantidad, Costo = 5, Precio = 10 };

    private static Venta VentaDePrueba(int cantidadDetalle = 3, decimal precio = 10)
    {
        var venta = new Venta { Id = 1, NumeroVenta = "VEN-000001", ClienteNombre = "Cliente final", Estado = EstadoDocumento.Borrador };
        venta.Detalles.Add(new VentaDetalle
        {
            ProductoId = 1, Cantidad = cantidadDetalle, PrecioUnitario = precio, CostoUnitarioSnapshot = 5,
            Subtotal = cantidadDetalle * precio, UtilidadBruta = cantidadDetalle * (precio - 5),
            ProductoNombreSnapshot = "Mouse", ProductoMarcaSnapshot = "Logitech", ProductoModeloSnapshot = "M185"
        });
        venta.Total = cantidadDetalle * precio;
        return venta;
    }

    [Fact]
    public async Task ConfirmarAsync_Reduce_Stock_Y_Guarda_Vendedor()
    {
        var producto = ProductoDePrueba(cantidad: 10);
        var venta = VentaDePrueba(cantidadDetalle: 3);

        _ventaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(venta);
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);

        var resultado = await _service.ConfirmarAsync(1);

        Assert.Equal(7, producto.Cantidad); // 10 - 3
        Assert.Equal("Confirmada", resultado!.Estado);
        Assert.Equal(4, venta.ConfirmadoPorUsuarioId);
        Assert.Equal("vendedor1", venta.ConfirmadoPorNombreUsuario);
    }

    [Fact]
    public async Task ConfirmarAsync_Sin_Stock_Suficiente_No_Confirma()
    {
        var producto = ProductoDePrueba(cantidad: 2); // solo 2 disponibles
        var venta = VentaDePrueba(cantidadDetalle: 5); // se piden 5

        _ventaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(venta);
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ConfirmarAsync(1));

        Assert.Equal(2, producto.Cantidad); // no debe haber cambiado
        Assert.Equal(EstadoDocumento.Borrador, venta.Estado); // no debe haberse confirmado
        _movInvRepoMock.Verify(r => r.AddAsync(It.IsAny<MovimientoInventario>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmarAsync_Genera_Factura_Con_Vendedor_Correcto()
    {
        var producto = ProductoDePrueba(cantidad: 10);
        var venta = VentaDePrueba(cantidadDetalle: 2);

        _ventaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(venta);
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);
        _facturaRepoMock.Setup(r => r.ContarTodasAsync()).ReturnsAsync(0);

        Factura? facturaCreada = null;
        _facturaRepoMock.Setup(r => r.AddAsync(It.IsAny<Factura>()))
            .Callback<Factura>(f => facturaCreada = f)
            .Returns(Task.CompletedTask);

        await _service.ConfirmarAsync(1);

        Assert.NotNull(facturaCreada);
        Assert.Equal("FAC-000001", facturaCreada!.NumeroFactura);
        Assert.Equal("Vendedor Uno", facturaCreada.VendedorNombreUsuario);
        Assert.Equal(4, facturaCreada.GeneradaPorUsuarioId);
    }

    [Fact]
    public async Task AnularAsync_Revierte_Stock_Y_Anula_Factura()
    {
        var producto = ProductoDePrueba(cantidad: 7); // después de vender 3 de 10
        var venta = VentaDePrueba(cantidadDetalle: 3);
        venta.Estado = EstadoDocumento.Confirmada;

        var factura = new Factura { Id = 1, VentaId = 1, NumeroFactura = "FAC-000001", Estado = EstadoFactura.Emitida };

        _ventaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(venta);
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);
        _facturaRepoMock.Setup(r => r.GetByVentaIdAsync(1)).ReturnsAsync(factura);

        var resultado = await _service.AnularAsync(1, "Cliente se arrepintió");

        Assert.Equal(10, producto.Cantidad); // 7 + 3
        Assert.Equal("Anulada", resultado!.Estado);
        Assert.Equal(EstadoFactura.Anulada, factura.Estado);
        Assert.Equal("Cliente se arrepintió", factura.MotivoAnulacion);
    }
}
