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
    private readonly Mock<IMovimientoInventarioRepository> _movInvRepoMock = new();
    private readonly Mock<IMovimientoFinancieroRepository> _movFinRepoMock = new();
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
            _movInvRepoMock.Object,
            _movFinRepoMock.Object,
            _currentUserMock.Object,
            new FakeUnitOfWork(),
            _auditoriaMock.Object);
    }

    private static Producto ProductoDePrueba(int id = 1, int cantidad = 10) =>
        new() { Id = id, Nombre = "Mouse", Marca = "Logitech", Modelo = "M185", Cantidad = cantidad, Costo = 5, Precio = 10 };

    [Fact]
    public async Task ConfirmarAsync_Aumenta_Stock_Y_Guarda_Usuario_Responsable()
    {
        var producto = ProductoDePrueba(cantidad: 10);
        var compra = new Compra
        {
            Id = 1,
            NumeroCompra = "COM-000001",
            ProveedorNombre = "Proveedor X",
            Estado = EstadoDocumento.Borrador
        };
        compra.Detalles.Add(new CompraDetalle { ProductoId = 1, Cantidad = 5, CostoUnitario = 4, Subtotal = 20, ProductoNombreSnapshot = "Mouse", ProductoMarcaSnapshot = "Logitech", ProductoModeloSnapshot = "M185" });
        compra.Total = 20;

        _compraRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(compra);
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);

        var resultado = await _service.ConfirmarAsync(1);

        Assert.Equal(15, producto.Cantidad); // 10 + 5
        Assert.Equal("Confirmada", resultado!.Estado);
        Assert.Equal(3, compra.ConfirmadoPorUsuarioId);
        Assert.Equal("comprador1", compra.ConfirmadoPorNombreUsuario);
        _movInvRepoMock.Verify(r => r.AddAsync(It.Is<MovimientoInventario>(m => m.Tipo == TipoMovimientoInventario.Entrada && m.Cantidad == 5)), Times.Once);
        _movFinRepoMock.Verify(r => r.AddAsync(It.Is<MovimientoFinanciero>(m => m.Categoria == CategoriaMovimientoFinanciero.Compra)), Times.Once);
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
        var compra = new Compra
        {
            Id = 1,
            NumeroCompra = "COM-000001",
            ProveedorNombre = "Proveedor X",
            Estado = EstadoDocumento.Confirmada,
            Total = 20
        };
        compra.Detalles.Add(new CompraDetalle { ProductoId = 1, Cantidad = 5, CostoUnitario = 4, Subtotal = 20, ProductoNombreSnapshot = "Mouse", ProductoMarcaSnapshot = "Logitech", ProductoModeloSnapshot = "M185" });

        _compraRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(compra);
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);

        var resultado = await _service.AnularAsync(1, "Producto dañado devuelto al proveedor");

        Assert.Equal(10, producto.Cantidad); // 15 - 5
        Assert.Equal("Anulada", resultado!.Estado);
        Assert.Equal("Producto dañado devuelto al proveedor", compra.MotivoAnulacion);
        Assert.Equal(3, compra.AnuladoPorUsuarioId);
    }

    [Fact]
    public async Task AnularAsync_Sin_Stock_Suficiente_Lanza_Excepcion()
    {
        var producto = ProductoDePrueba(cantidad: 2); // ya se vendieron 3 de las 5 originales
        var compra = new Compra { Id = 1, NumeroCompra = "COM-000001", Estado = EstadoDocumento.Confirmada, Total = 20 };
        compra.Detalles.Add(new CompraDetalle { ProductoId = 1, Cantidad = 5, CostoUnitario = 4, Subtotal = 20, ProductoNombreSnapshot = "Mouse", ProductoMarcaSnapshot = "Logitech", ProductoModeloSnapshot = "M185" });

        _compraRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(compra);
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AnularAsync(1, "motivo"));
    }

    [Fact]
    public async Task AnularAsync_Sin_Motivo_Lanza_Excepcion()
    {
        var compra = new Compra { Id = 1, Estado = EstadoDocumento.Confirmada };
        _compraRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(compra);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AnularAsync(1, ""));
    }

    [Fact]
    public async Task CreateAsync_Calcula_Totales_Correctamente()
    {
        var producto = ProductoDePrueba();
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);
        _compraRepoMock.Setup(r => r.ContarTodasAsync()).ReturnsAsync(0);

        Compra? creada = null;
        _compraRepoMock.Setup(r => r.AddAsync(It.IsAny<Compra>()))
            .Callback<Compra>(c => creada = c)
            .Returns(Task.CompletedTask);

        var dto = new CreateCompraDto
        {
            ProveedorNombre = "Proveedor X",
            Descuento = 5,
            Impuesto = 3,
            Detalles = new List<CompraDetalleInputDto> { new() { ProductoId = 1, Cantidad = 2, CostoUnitario = 10 } }
        };

        await _service.CreateAsync(dto);

        Assert.NotNull(creada);
        Assert.Equal(20, creada!.Subtotal);   // 2 * 10
        Assert.Equal(18, creada.Total);       // 20 - 5 + 3
        Assert.Equal("COM-000001", creada.NumeroCompra);
        Assert.Equal(EstadoDocumento.Borrador, creada.Estado);
    }
}
