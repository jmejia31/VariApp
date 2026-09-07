using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Common;
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
    private readonly Mock<IProductoVarianteRepository> _varianteRepoMock = new();
    private readonly Mock<IInventarioConcurrencyService> _inventarioConcurrencyMock = new();
    private readonly Mock<IFacturaRepository> _facturaRepoMock = new();
    private readonly Mock<IMovimientoInventarioRepository> _movInvRepoMock = new();
    private readonly Mock<IMovimientoFinancieroRepository> _movFinRepoMock = new();
    private readonly Mock<IEmpresaConfiguracionService> _empresaMock = new();
    private readonly Mock<ICalculoService> _calculoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IAuditoriaService> _auditoriaMock = new();
    private readonly Mock<ITipoClientePredeterminadoResolver> _predeterminadoResolverMock = new();
    private readonly VentaService _service;

    public VentaServiceTests()
    {
        _currentUserMock.Setup(c => c.UsuarioId).Returns(4);
        _currentUserMock.Setup(c => c.NombreUsuario).Returns("vendedor1");
        _currentUserMock.Setup(c => c.NombreCompleto).Returns("Vendedor Uno");
        _ventaRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
        _empresaMock.Setup(e => e.GetActivaEntidadAsync()).ReturnsAsync(new EmpresaConfiguracion());
        _predeterminadoResolverMock.Setup(r => r.ResolverIdPredeterminadoAsync()).ReturnsAsync(1);

        _service = new VentaService(
            _ventaRepoMock.Object,
            _clienteRepoMock.Object,
            _productoRepoMock.Object,
            _varianteRepoMock.Object,
            _inventarioConcurrencyMock.Object,
            _facturaRepoMock.Object,
            _movInvRepoMock.Object,
            _movFinRepoMock.Object,
            _empresaMock.Object,
            _calculoMock.Object,
            _currentUserMock.Object,
            new FakeUnitOfWork(),
            _auditoriaMock.Object,
            _predeterminadoResolverMock.Object);
    }

    private static Producto ProductoDePrueba(int id = 1, int cantidad = 10) =>
        new() { Id = id, Nombre = "Mouse", Marca = "Logitech", Modelo = "M185", Cantidad = cantidad, Costo = 5, Precio = 10 };

    private static Venta VentaDePrueba(int cantidadDetalle = 3, decimal precio = 10, int? varianteId = null)
    {
        var venta = new Venta { Id = 1, NumeroVenta = "VEN-000001", ClienteNombre = "Cliente final", Estado = EstadoDocumento.Borrador };
        venta.Detalles.Add(new VentaDetalle
        {
            ProductoId = 1,
            ProductoVarianteId = varianteId,
            Cantidad = cantidadDetalle,
            PrecioUnitario = precio,
            CostoUnitarioSnapshot = 5,
            Subtotal = cantidadDetalle * precio,
            UtilidadBruta = cantidadDetalle * (precio - 5),
            ProductoNombreSnapshot = "Mouse",
            ProductoMarcaSnapshot = "Logitech",
            ProductoModeloSnapshot = "M185",
            ProductoColorSnapshot = varianteId.HasValue ? "Negro" : null,
            ProductoSkuSnapshot = varianteId.HasValue ? "M185-BLK" : null
        });
        venta.Total = cantidadDetalle * precio;
        return venta;
    }

    private void PrepararBloqueos(Venta venta, Producto producto, ProductoVariante? variante = null)
    {
        _ventaRepoMock.Setup(r => r.GetByIdForUpdateAsync(venta.Id)).ReturnsAsync(venta);
        _ventaRepoMock.Setup(r => r.GetByIdAsync(venta.Id)).ReturnsAsync(venta);

        var productos = new Dictionary<int, Producto> { [producto.Id] = producto };
        var variantes = variante is null
            ? new Dictionary<int, ProductoVariante>()
            : new Dictionary<int, ProductoVariante> { [variante.Id] = variante };

        _inventarioConcurrencyMock
            .Setup(c => c.BloquearYValidarInventarioAsync(
                It.IsAny<IEnumerable<InventarioDemanda>>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new InventarioLockSet(
                productos,
                variantes,
                venta.Detalles
                    .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, d.Cantidad))
                    .ToList()));
    }

    [Fact]
    public async Task ConfirmarAsync_Reduce_Stock_Y_Guarda_Vendedor()
    {
        var producto = ProductoDePrueba(cantidad: 10);
        var venta = VentaDePrueba(cantidadDetalle: 3);
        PrepararBloqueos(venta, producto);

        var resultado = await _service.ConfirmarAsync(1);

        Assert.Equal(7, producto.Cantidad);
        Assert.Equal("Confirmada", resultado!.Estado);
        Assert.Equal(4, venta.ConfirmadoPorUsuarioId);
        Assert.Equal("vendedor1", venta.ConfirmadoPorNombreUsuario);
        _movInvRepoMock.Verify(r => r.AddConOrigenTipadoAsync(
            It.Is<MovimientoInventario>(m => m.Tipo == TipoMovimientoInventario.Salida && m.Cantidad == 3),
            It.Is<OrigenMovimientoInventario>(o => o.VentaId == venta.Id && o.CompraId == null && o.ConsumoInsumoId == null)), Times.Once);
        _movInvRepoMock.Verify(r => r.AddAsync(It.IsAny<MovimientoInventario>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmarAsync_Variante_Reduce_Stock_Exacto_Y_Consolidado()
    {
        var producto = ProductoDePrueba(cantidad: 10);
        var variante = new ProductoVariante { Id = 8, ProductoId = 1, Sku = "M185-BLK", Cantidad = 6, Activo = true };
        var venta = VentaDePrueba(cantidadDetalle: 2, varianteId: variante.Id);
        PrepararBloqueos(venta, producto, variante);

        var resultado = await _service.ConfirmarAsync(1);

        Assert.Equal(4, variante.Cantidad);
        Assert.Equal(8, producto.Cantidad);
        Assert.Equal("M185-BLK", resultado!.Detalles.Single().ProductoSku);
        _movInvRepoMock.Verify(r => r.AddConOrigenTipadoAsync(
            It.Is<MovimientoInventario>(m => m.ProductoVarianteId == 8 && m.StockAnterior == 6 && m.StockNuevo == 4),
            It.Is<OrigenMovimientoInventario>(o => o.VentaId == venta.Id)), Times.Once);
    }

    [Fact]
    public async Task ConfirmarAsync_Variante_Sin_Stock_No_Confirma()
    {
        var producto = ProductoDePrueba(cantidad: 10);
        var variante = new ProductoVariante { Id = 8, ProductoId = 1, Sku = "M185-BLK", Cantidad = 1, Activo = true };
        var venta = VentaDePrueba(cantidadDetalle: 2, varianteId: variante.Id);
        PrepararBloqueos(venta, producto, variante);
        _inventarioConcurrencyMock
            .Setup(c => c.BloquearYValidarInventarioAsync(It.IsAny<IEnumerable<InventarioDemanda>>(), true))
            .ThrowsAsync(new BusinessRuleException("Stock insuficiente."));

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ConfirmarAsync(1));
        Assert.Equal(1, variante.Cantidad);
        Assert.Equal(10, producto.Cantidad);
    }

    [Fact]
    public async Task ConfirmarAsync_Sin_Stock_Suficiente_No_Confirma()
    {
        var producto = ProductoDePrueba(cantidad: 2);
        var venta = VentaDePrueba(cantidadDetalle: 5);
        PrepararBloqueos(venta, producto);
        _inventarioConcurrencyMock
            .Setup(c => c.BloquearYValidarInventarioAsync(It.IsAny<IEnumerable<InventarioDemanda>>(), true))
            .ThrowsAsync(new BusinessRuleException("Stock insuficiente."));

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ConfirmarAsync(1));

        Assert.Equal(2, producto.Cantidad);
        Assert.Equal(EstadoDocumento.Borrador, venta.Estado);
        _movInvRepoMock.Verify(r => r.AddConOrigenTipadoAsync(It.IsAny<MovimientoInventario>(), It.IsAny<OrigenMovimientoInventario>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmarAsync_Genera_Factura_Con_Vendedor_Correcto()
    {
        var producto = ProductoDePrueba(cantidad: 10);
        var venta = VentaDePrueba(cantidadDetalle: 2);
        PrepararBloqueos(venta, producto);
        Factura? facturaCreada = null;
        _facturaRepoMock.Setup(r => r.AddAsync(It.IsAny<Factura>()))
            .Callback<Factura>(f => { f.Id = 1; facturaCreada = f; })
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
        var producto = ProductoDePrueba(cantidad: 7);
        var venta = VentaDePrueba(cantidadDetalle: 3);
        venta.Estado = EstadoDocumento.Confirmada;
        var factura = new Factura { Id = 1, VentaId = 1, NumeroFactura = "FAC-000001", Estado = EstadoFactura.Emitida };
        PrepararBloqueos(venta, producto);
        _facturaRepoMock.Setup(r => r.GetByVentaIdAsync(1)).ReturnsAsync(factura);

        var resultado = await _service.AnularAsync(1, "Cliente se arrepintió");

        Assert.Equal(10, producto.Cantidad);
        Assert.Equal("Anulada", resultado!.Estado);
        Assert.Equal(EstadoFactura.Anulada, factura.Estado);
        Assert.Equal("Cliente se arrepintió", factura.MotivoAnulacion);
        _movInvRepoMock.Verify(r => r.AddConOrigenTipadoAsync(
            It.Is<MovimientoInventario>(m => m.Causa == CausaMovimientoInventario.AnulacionVenta && m.Tipo == TipoMovimientoInventario.Entrada),
            It.Is<OrigenMovimientoInventario>(o => o.VentaId == venta.Id)), Times.Once);
    }

    [Fact]
    public async Task AnularAsync_Variante_Restaura_Stock_Exacto()
    {
        var producto = ProductoDePrueba(cantidad: 8);
        var variante = new ProductoVariante { Id = 8, ProductoId = 1, Sku = "M185-BLK", Cantidad = 4, Activo = false };
        var venta = VentaDePrueba(cantidadDetalle: 2, varianteId: 8);
        venta.Estado = EstadoDocumento.Confirmada;
        PrepararBloqueos(venta, producto, variante);

        await _service.AnularAsync(1, "Anulación controlada");

        Assert.Equal(6, variante.Cantidad);
        Assert.Equal(10, producto.Cantidad);
    }

    [Fact]
    public async Task GetByIdAsync_Incluye_Imagen_Principal_Del_Producto()
    {
        var producto = ProductoDePrueba();
        producto.Imagenes.Add(new ProductoImagen { Id = 11, Url = "https://res.cloudinary.com/demo/image/upload/producto-venta.webp", EsPrincipal = true, Orden = 0 });
        var venta = VentaDePrueba(cantidadDetalle: 1);
        venta.Detalles.Single().Producto = producto;
        _ventaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(venta);

        var resultado = await _service.GetByIdAsync(1);

        Assert.Equal("https://res.cloudinary.com/demo/image/upload/producto-venta.webp", resultado!.Detalles.Single().ProductoImagenPrincipalUrl);
    }
}
