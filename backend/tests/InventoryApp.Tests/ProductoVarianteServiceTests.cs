using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class ProductoVarianteServiceTests
{
    private readonly Mock<IProductoVarianteRepository> _repository = new();
    private readonly Mock<IProductoRepository> _productoRepository = new();
    private readonly Mock<ICatalogoProductoService> _catalogoService = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly ProductoVarianteService _service;
    private readonly Producto _producto;
    private readonly CatalogoProductoDto _color;

    public ProductoVarianteServiceTests()
    {
        _producto = new Producto { Id = 1, Nombre = "Cobertor", Marca = "Samsung", Modelo = "S24 Ultra", Cantidad = 0, Costo = 0, Precio = 220, Activo = true };
        _color = new CatalogoProductoDto { Id = 9, Tipo = "Color", Nombre = "Negro", CodigoVisual = "#111111", Activo = true };
        _currentUser.SetupGet(x => x.UsuarioId).Returns(2);
        _currentUser.SetupGet(x => x.NombreUsuario).Returns("admin");
        _productoRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_producto);
        _productoRepository.Setup(r => r.GetByIdForUpdateAsync(1)).ReturnsAsync(_producto);
        _productoRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
        _repository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
        _repository.Setup(r => r.GetByProductoIdAsync(1, true)).ReturnsAsync(new List<ProductoVariante>());
        _catalogoService.Setup(s => s.GetByIdAsync(TipoCatalogoProducto.Color, 9)).ReturnsAsync(_color);

        _service = new ProductoVarianteService(
            _repository.Object,
            _productoRepository.Object,
            _catalogoService.Object,
            _currentUser.Object,
            new FakeUnitOfWork(),
            _auditoria.Object);
    }

    [Fact]
    public async Task CreateAsync_Normaliza_Sku_Y_Registra_Variante()
    {
        ProductoVariante? capturada = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<ProductoVariante>()))
            .Callback<ProductoVariante>(v => { capturada = v; v.Id = 31; })
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.GetByIdAsync(31)).ReturnsAsync(() =>
        {
            capturada!.Producto = _producto;
            capturada.Color = new CatalogoProducto { Id = 9, Nombre = "Negro", CodigoVisual = "#111111" };
            return capturada;
        });
        _repository.Setup(r => r.GetByProductoIdAsync(1, true)).ReturnsAsync(() => capturada is null ? new() : new() { capturada });

        var resultado = await _service.CreateAsync(1, new CreateProductoVarianteDto
        {
            ColorId = 9,
            Sku = " case-s24u-blk ",
            CodigoBarras = " 123456 ",
            Cantidad = 4,
            UmbralStockBajo = 1,
            Costo = 100,
            Precio = 220
        });

        Assert.Equal("CASE-S24U-BLK", resultado.Sku);
        Assert.Equal("123456", resultado.CodigoBarras);
        Assert.Equal(4, resultado.Cantidad);
        Assert.Equal(4, _producto.Cantidad);
        _auditoria.Verify(a => a.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Crear,
            It.IsAny<string>(),
            31,
            "ProductoVariante",
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Sku_Duplicado_Lanza_Excepcion()
    {
        _repository.Setup(r => r.GetBySkuAsync("CASE-S24U-BLK"))
            .ReturnsAsync(new ProductoVariante { Id = 77, ProductoId = 2, Sku = "CASE-S24U-BLK" });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateAsync(1, new CreateProductoVarianteDto
        {
            ColorId = 9,
            Sku = "CASE-S24U-BLK",
            Cantidad = 0,
            Costo = 100,
            Precio = 220
        }));

        _repository.Verify(r => r.AddAsync(It.IsAny<ProductoVariante>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Con_Stock_No_Elimina()
    {
        var variante = new ProductoVariante
        {
            Id = 5,
            ProductoId = 1,
            Sku = "CASE-S24U-BLK",
            Cantidad = 2,
            Activo = true
        };
        _repository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(variante);
        _repository.Setup(r => r.GetByIdForUpdateAsync(5)).ReturnsAsync(variante);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.DeleteAsync(1, 5));
        _repository.Verify(r => r.Update(It.IsAny<ProductoVariante>()), Times.Never);
    }

    [Fact]
    public async Task CambiarEstadoAsync_Conserva_Stock_Fisico_Y_Usa_Precio_Activo()
    {
        var negra = new ProductoVariante { Id = 5, ProductoId = 1, Sku = "BLACK", Cantidad = 3, Costo = 90, Precio = 210, Activo = true };
        var azul = new ProductoVariante { Id = 6, ProductoId = 1, Sku = "BLUE", Cantidad = 2, Costo = 110, Precio = 230, Activo = true };
        _repository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(negra);
        _repository.Setup(r => r.GetByIdForUpdateAsync(5)).ReturnsAsync(negra);
        _repository.Setup(r => r.GetByProductoIdAsync(1, true)).ReturnsAsync(new List<ProductoVariante> { negra, azul });

        await _service.CambiarEstadoAsync(1, 5, false);

        Assert.False(negra.Activo);
        Assert.Equal(5, _producto.Cantidad);
        Assert.Equal(98, _producto.Costo);
        Assert.Equal(230, _producto.Precio);
    }
}
