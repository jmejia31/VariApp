using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class CompraServiceTests
{
    private readonly Mock<ICompraRepository> _compraRepoMock = new();
    private readonly Mock<IProveedorRepository> _proveedorRepoMock = new();
    private readonly Mock<IProductoRepository> _productoRepoMock = new();
    private readonly Mock<IProductoVarianteRepository> _varianteRepoMock = new();
    private readonly Mock<IMovimientoInventarioRepository> _movInvRepoMock = new();
    private readonly Mock<IMovimientoFinancieroRepository> _movFinRepoMock = new();
    private readonly Mock<ICalculoService> _calculoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IAuditoriaService> _auditoriaMock = new();
    private readonly CompraService _service;

    public CompraServiceTests()
    {
        _currentUserMock.Setup(c => c.UsuarioId).Returns(3);
        _currentUserMock.Setup(c => c.NombreUsuario).Returns("comprador1");
        _compraRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        _service = new CompraService(
            _compraRepoMock.Object,
            _proveedorRepoMock.Object,
            _productoRepoMock.Object,
            _varianteRepoMock.Object,
            _movInvRepoMock.Object,
            _movFinRepoMock.Object,
            _calculoMock.Object,
            _currentUserMock.Object,
            new FakeUnitOfWork(),
            _auditoriaMock.Object);
    }

    private static Producto ProductoDePrueba(int id = 1, int cantidad = 10) =>
        new() { Id = id, Nombre = "Mouse", Marca = "Logitech", Modelo = "M185", Cantidad = cantidad, Costo = 5, Precio = 10 };

    private static Compra CompraDePrueba(int cantidad = 5, int? varianteId = null, EstadoDocumento estado = EstadoDocumento.Borrador)
    {
        var compra = new Compra { Id = 1, NumeroCompra = "COM-000001", ProveedorNombre = "Proveedor X", Estado = estado, Total = 20 };
        compra.Detalles.Add(new CompraDetalle
        {
            ProductoId = 1,
            ProductoVarianteId = varianteId,
            Cantidad = cantidad,
            CostoUnitario = 4,
            Subtotal = cantidad * 4,
            ProductoNombreSnapshot = "Mouse",
            ProductoMarcaSnapshot = "Logitech",
            ProductoModeloSnapshot = "M185",
            ProductoColorSnapshot = varianteId.HasValue ? "Negro" : null,
            ProductoSkuSnapshot = varianteId.HasValue ? "M185-BLK" : null
        });
        return compra;
    }

    [Fact]
    public async Task ConfirmarAsync_Aumenta_Stock_Y_Guarda_Usuario_Responsable()
    {
        var producto = ProductoDePrueba(cantidad: 10);
        var compra = CompraDePrueba();
        _compraRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(compra);
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);

        var resultado = await _service.ConfirmarAsync(1);

        Assert.Equal(15, producto.Cantidad);
        Assert.Equal("Confirmada", resultado!.Estado);
        Assert.Equal(3, compra.ConfirmadoPorUsuarioId);
        Assert.Equal("comprador1", compra.ConfirmadoPorNombreUsuario);
        _movInvRepoMock.Verify(r => r.AddAsync(It.Is<MovimientoInventario>(m => m.Tipo == TipoMovimientoInventario.Entrada && m.Cantidad == 5)), Times.Once);
    }

    [Fact]
    public async Task ConfirmarAsync_Variante_Aumenta_Stock_Exacto_Y_Consolidado()
    {
        var producto = ProductoDePrueba(cantidad: 10);
        var variante = new ProductoVariante { Id = 8, ProductoId = 1, Sku = "M185-BLK", Cantidad = 3, Costo = 4, Precio = 10, Activo = true };
        var compra = CompraDePrueba(cantidad: 5, varianteId: 8);
        _compraRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(compra);
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);
        _varianteRepoMock.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(variante);

        var resultado = await _service.ConfirmarAsync(1);

        Assert.Equal(8, variante.Cantidad);
        Assert.Equal(15, producto.Cantidad);
        Assert.Equal("M185-BLK", resultado!.Detalles.Single().ProductoSku);
        _movInvRepoMock.Verify(r => r.AddAsync(It.Is<MovimientoInventario>(m =>
            m.ProductoVarianteId == 8 && m.StockAnterior == 3 && m.StockNuevo == 8)), Times.Once);
    }

    [Fact]
    public async Task ConfirmarAsync_Compra_Ya_Confirmada_Lanza_Excepcion()
    {
        var compra = new Compra { Id = 1, Estado = EstadoDocumento.Confirmada };
        _compraRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(compra);
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ConfirmarAsync(1));
    }

    [Fact]
    public async Task AnularAsync_Revierte_Stock_Y_Guarda_Motivo()
    {
        var producto = ProductoDePrueba(cantidad: 15);
        var compra = CompraDePrueba(estado: EstadoDocumento.Confirmada);
        _compraRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(compra);
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);

        var resultado = await _service.AnularAsync(1, "Producto dañado devuelto al proveedor");

        Assert.Equal(10, producto.Cantidad);
        Assert.Equal("Anulada", resultado!.Estado);
        Assert.Equal("Producto dañado devuelto al proveedor", compra.MotivoAnulacion);
        Assert.Equal(3, compra.AnuladoPorUsuarioId);
    }

    [Fact]
    public async Task AnularAsync_Variante_Revierte_Stock_Exacto()
    {
        var producto = ProductoDePrueba(cantidad: 15);
        var variante = new ProductoVariante { Id = 8, ProductoId = 1, Sku = "M185-BLK", Cantidad = 8, Activo = false };
        var compra = CompraDePrueba(cantidad: 5, varianteId: 8, estado: EstadoDocumento.Confirmada);
        _compraRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(compra);
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);
        _varianteRepoMock.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(variante);

        await _service.AnularAsync(1, "Reversión controlada");

        Assert.Equal(3, variante.Cantidad);
        Assert.Equal(10, producto.Cantidad);
    }

    [Fact]
    public async Task AnularAsync_Variante_Sin_Stock_Suficiente_Lanza_Excepcion()
    {
        var producto = ProductoDePrueba(cantidad: 12);
        var variante = new ProductoVariante { Id = 8, ProductoId = 1, Sku = "M185-BLK", Cantidad = 2, Activo = true };
        var compra = CompraDePrueba(cantidad: 5, varianteId: 8, estado: EstadoDocumento.Confirmada);
        _compraRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(compra);
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);
        _varianteRepoMock.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(variante);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AnularAsync(1, "motivo"));
        Assert.Equal(2, variante.Cantidad);
    }

    [Fact]
    public async Task AnularAsync_Sin_Motivo_Lanza_Excepcion()
    {
        var compra = new Compra { Id = 1, Estado = EstadoDocumento.Confirmada };
        _compraRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(compra);
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AnularAsync(1, ""));
    }

    [Fact]
    public async Task CreateAsync_Recalcula_Totales_Con_Motor_De_Calculo()
    {
        var producto = ProductoDePrueba();
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);
        _compraRepoMock.Setup(r => r.ContarTodasAsync()).ReturnsAsync(0);
        _calculoMock.Setup(c => c.CalcularCompraAsync(It.IsAny<List<DetalleCalculoInput>>(), It.IsAny<int?>()))
            .ReturnsAsync(new ResultadoCalculoDto { Subtotal = 20, TotalDescuento = 0, TotalImpuesto = 3, Total = 23 });
        Compra? creada = null;
        _compraRepoMock.Setup(r => r.AddAsync(It.IsAny<Compra>())).Callback<Compra>(c => creada = c).Returns(Task.CompletedTask);
        var dto = new CreateCompraDto
        {
            ProveedorNombre = "Proveedor X",
            Detalles = new List<CompraDetalleInputDto> { new() { ProductoId = 1, Cantidad = 2, CostoUnitario = 10 } }
        };

        await _service.CreateAsync(dto);

        Assert.NotNull(creada);
        Assert.Equal(20, creada!.Subtotal);
        Assert.Equal(0, creada.Descuento);
        Assert.Equal(3, creada.Impuesto);
        Assert.Equal(23, creada.Total);
    }

    [Fact]
    public async Task GetByIdAsync_Incluye_Imagen_Principal_Del_Producto()
    {
        var producto = ProductoDePrueba();
        producto.Imagenes.Add(new ProductoImagen { Id = 10, Url = "https://res.cloudinary.com/demo/image/upload/producto-principal.webp", EsPrincipal = true, Orden = 0 });
        var compra = new Compra { Id = 7, NumeroCompra = "COM-000007", ProveedorNombre = "Proveedor X" };
        compra.Detalles.Add(new CompraDetalle
        {
            Id = 4, ProductoId = producto.Id, Producto = producto, ProductoNombreSnapshot = producto.Nombre,
            ProductoMarcaSnapshot = producto.Marca, ProductoModeloSnapshot = producto.Modelo,
            Cantidad = 1, CostoUnitario = 5, Subtotal = 5
        });
        _compraRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(compra);

        var resultado = await _service.GetByIdAsync(7);

        Assert.Equal("https://res.cloudinary.com/demo/image/upload/producto-principal.webp", resultado!.Detalles.Single().ProductoImagenPrincipalUrl);
    }
}
