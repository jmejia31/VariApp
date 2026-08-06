using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class InventarioAjusteServiceTests
{
    private readonly Mock<IInventarioConcurrencyService> _concurrency = new();
    private readonly Mock<IMovimientoInventarioRepository> _movimientos = new();
    private readonly Mock<IProductoRepository> _productos = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly InventarioAjusteService _service;

    public InventarioAjusteServiceTests()
    {
        _currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        _currentUser.SetupGet(x => x.NombreUsuario).Returns("inventario-admin");
        _productos.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        _service = new InventarioAjusteService(
            _concurrency.Object,
            _movimientos.Object,
            _productos.Object,
            _currentUser.Object,
            new FakeUnitOfWork(),
            _auditoria.Object);
    }

    [Fact]
    public async Task AjustarProductoAsync_RegistraMovimientoAjuste()
    {
        MovimientoInventario? movimiento = null;
        _movimientos.Setup(x => x.AddAsync(It.IsAny<MovimientoInventario>()))
            .Callback<MovimientoInventario>(x => movimiento = x)
            .Returns(Task.CompletedTask);

        var resultado = await _service.AjustarProductoAsync(10, new AjusteStockRequest
        {
            CantidadActualEsperada = 8,
            CantidadNueva = 5,
            Motivo = "Conteo físico"
        });

        Assert.Equal(-3, resultado.Diferencia);
        Assert.NotNull(movimiento);
        Assert.Equal(TipoMovimientoInventario.Ajuste, movimiento!.Tipo);
        Assert.Equal(3, movimiento.Cantidad);
        Assert.Equal(8, movimiento.StockAnterior);
        Assert.Equal(5, movimiento.StockNuevo);
        Assert.Equal("AjusteProducto", movimiento.ReferenciaTipo);
        _concurrency.Verify(x => x.AjustarStockPesimistaAsync(10, null, 8, 5), Times.Once);
    }

    [Fact]
    public async Task AjustarVarianteAsync_PropagaConflictoDeStockObsoleto()
    {
        _concurrency.Setup(x => x.AjustarStockPesimistaAsync(10, 4, 8, 5))
            .ThrowsAsync(new BusinessRuleException(
                "El inventario cambió desde que se cargó el formulario."));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.AjustarVarianteAsync(10, 4, new AjusteStockRequest
            {
                CantidadActualEsperada = 8,
                CantidadNueva = 5,
                Motivo = "Conteo"
            }));

        _movimientos.Verify(x => x.AddAsync(It.IsAny<MovimientoInventario>()), Times.Never);
    }

    [Theory]
    [InlineData(-1, 0, "motivo")]
    [InlineData(0, -1, "motivo")]
    [InlineData(0, 0, "")]
    public async Task AjustarProductoAsync_ValidaSolicitud(
        int esperada,
        int nueva,
        string motivo)
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.AjustarProductoAsync(10, new AjusteStockRequest
            {
                CantidadActualEsperada = esperada,
                CantidadNueva = nueva,
                Motivo = motivo
            }));
    }
}
