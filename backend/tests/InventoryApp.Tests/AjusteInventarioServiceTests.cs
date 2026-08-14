using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Common;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class AjusteInventarioServiceTests
{
    [Fact]
    public async Task ConfirmarAsync_MaterializaSnapshots_AjustaStock_YRegistraOrigenTipado()
    {
        var fixture = new Fixture();
        var producto = new Producto
        {
            Id = 10,
            Nombre = "Producto prueba",
            Marca = "Marca",
            Modelo = "Modelo",
            Cantidad = 5,
            Costo = 2.50m
        };
        var ajuste = CrearBorrador(7, producto.Id, cantidadObjetivo: 8);

        fixture.Ajustes.Setup(x => x.GetByIdForUpdateAsync(ajuste.Id)).ReturnsAsync(ajuste);
        fixture.Ajustes.Setup(x => x.GetByIdAsync(ajuste.Id)).ReturnsAsync(ajuste);
        fixture.Productos.Setup(x => x.GetByIdAsync(producto.Id)).ReturnsAsync(producto);
        fixture.Concurrency
            .Setup(x => x.BloquearInventarioParaReversionAsync(It.IsAny<IEnumerable<InventarioDemanda>>()))
            .ReturnsAsync(new InventarioLockSet(
                new Dictionary<int, Producto> { [producto.Id] = producto },
                new Dictionary<int, ProductoVariante>()));

        MovimientoInventario? movimiento = null;
        OrigenMovimientoInventario? origen = null;
        fixture.Movimientos
            .Setup(x => x.AddConOrigenTipadoAsync(It.IsAny<MovimientoInventario>(), It.IsAny<OrigenMovimientoInventario>()))
            .Callback<MovimientoInventario, OrigenMovimientoInventario>((m, o) =>
            {
                movimiento = m;
                origen = o;
            })
            .Returns(Task.CompletedTask);

        var resultado = await fixture.Service.ConfirmarAsync(ajuste.Id);

        Assert.NotNull(resultado);
        Assert.Equal(EstadoAjusteInventario.Confirmado, ajuste.Estado);
        Assert.Equal(8, producto.Cantidad);
        var detalle = Assert.Single(ajuste.Detalles);
        Assert.Equal(5, detalle.CantidadAnteriorSnapshot);
        Assert.Equal(8, detalle.CantidadNuevaSnapshot);
        Assert.Equal(3, detalle.DiferenciaSnapshot);
        Assert.Equal(2.50m, detalle.CostoUnitarioSnapshot);
        Assert.Equal(7.50m, detalle.ImpactoCostoSnapshot);
        Assert.Equal("Producto prueba", detalle.NombreSnapshot);
        Assert.NotNull(movimiento);
        Assert.Equal(TipoMovimientoInventario.Ajuste, movimiento!.Tipo);
        Assert.Equal(CausaMovimientoInventario.AjusteManual, movimiento.Causa);
        Assert.Equal(3, movimiento.Cantidad);
        Assert.Equal(5, movimiento.StockAnterior);
        Assert.Equal(8, movimiento.StockNuevo);
        Assert.NotNull(origen);
        Assert.Equal(ajuste.Id, origen!.AjusteInventarioId);
        Assert.Null(origen.CompraId);
        Assert.Null(origen.VentaId);
        Assert.Null(origen.ConsumoInsumoId);
    }

    [Fact]
    public async Task AnularAsync_AplicaMovimientoInversoSobreStockActual_SinReescribirHistoria()
    {
        var fixture = new Fixture();
        var producto = new Producto
        {
            Id = 10,
            Nombre = "Producto prueba",
            Cantidad = 10,
            Costo = 2m
        };
        var ajuste = CrearBorrador(7, producto.Id, cantidadObjetivo: 8);
        var detalle = Assert.Single(ajuste.Detalles);
        detalle.MaterializarConfirmacion(cantidadAnterior: 5, costoUnitario: 2m);
        ajuste.Confirmar(99, "tester", DateTime.UtcNow.AddMinutes(-5));

        fixture.Ajustes.Setup(x => x.GetByIdForUpdateAsync(ajuste.Id)).ReturnsAsync(ajuste);
        fixture.Ajustes.Setup(x => x.GetByIdAsync(ajuste.Id)).ReturnsAsync(ajuste);
        fixture.Concurrency
            .Setup(x => x.BloquearInventarioParaReversionAsync(It.IsAny<IEnumerable<InventarioDemanda>>()))
            .ReturnsAsync(new InventarioLockSet(
                new Dictionary<int, Producto> { [producto.Id] = producto },
                new Dictionary<int, ProductoVariante>()));

        MovimientoInventario? movimiento = null;
        OrigenMovimientoInventario? origen = null;
        fixture.Movimientos
            .Setup(x => x.AddConOrigenTipadoAsync(It.IsAny<MovimientoInventario>(), It.IsAny<OrigenMovimientoInventario>()))
            .Callback<MovimientoInventario, OrigenMovimientoInventario>((m, o) =>
            {
                movimiento = m;
                origen = o;
            })
            .Returns(Task.CompletedTask);

        var resultado = await fixture.Service.AnularAsync(ajuste.Id, "Conteo corregido");

        Assert.NotNull(resultado);
        Assert.Equal(EstadoAjusteInventario.Anulado, ajuste.Estado);
        Assert.Equal(7, producto.Cantidad);
        Assert.Equal(5, detalle.CantidadAnteriorSnapshot);
        Assert.Equal(8, detalle.CantidadNuevaSnapshot);
        Assert.NotNull(movimiento);
        Assert.Equal(TipoMovimientoInventario.Reversion, movimiento!.Tipo);
        Assert.Equal(3, movimiento.Cantidad);
        Assert.Equal(10, movimiento.StockAnterior);
        Assert.Equal(7, movimiento.StockNuevo);
        Assert.Equal(ajuste.Id, origen!.AjusteInventarioId);
    }

    [Fact]
    public async Task ConfirmarAsync_SegundaConfirmacion_FallaCerradoSinNuevoMovimiento()
    {
        var fixture = new Fixture();
        var ajuste = CrearBorrador(7, productoId: 10, cantidadObjetivo: 8);
        var detalle = Assert.Single(ajuste.Detalles);
        detalle.MaterializarConfirmacion(cantidadAnterior: 5, costoUnitario: 2m);
        ajuste.Confirmar(99, "tester", DateTime.UtcNow);
        fixture.Ajustes.Setup(x => x.GetByIdForUpdateAsync(ajuste.Id)).ReturnsAsync(ajuste);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => fixture.Service.ConfirmarAsync(ajuste.Id));

        Assert.Contains("Borrador", ex.Message);
        fixture.Movimientos.Verify(
            x => x.AddConOrigenTipadoAsync(It.IsAny<MovimientoInventario>(), It.IsAny<OrigenMovimientoInventario>()),
            Times.Never);
    }

    [Fact]
    public async Task AnularAsync_SegundaAnulacion_FallaCerradoSinNuevoMovimiento()
    {
        var fixture = new Fixture();
        var ajuste = CrearBorrador(7, productoId: 10, cantidadObjetivo: 8);
        var detalle = Assert.Single(ajuste.Detalles);
        detalle.MaterializarConfirmacion(cantidadAnterior: 5, costoUnitario: 2m);
        ajuste.Confirmar(99, "tester", DateTime.UtcNow.AddMinutes(-2));
        ajuste.Anular(99, "tester", "Primera anulación", DateTime.UtcNow.AddMinutes(-1));
        fixture.Ajustes.Setup(x => x.GetByIdForUpdateAsync(ajuste.Id)).ReturnsAsync(ajuste);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            fixture.Service.AnularAsync(ajuste.Id, "Reintento duplicado"));

        Assert.Contains("confirmados", ex.Message, StringComparison.OrdinalIgnoreCase);
        fixture.Movimientos.Verify(
            x => x.AddConOrigenTipadoAsync(It.IsAny<MovimientoInventario>(), It.IsAny<OrigenMovimientoInventario>()),
            Times.Never);
    }

    private static AjusteInventario CrearBorrador(int id, int productoId, int cantidadObjetivo)
    {
        var ajuste = new AjusteInventario
        {
            Id = id,
            NumeroAjuste = $"AI-{id:D6}",
            Motivo = "Conteo físico"
        };
        ajuste.Detalles.Add(new AjusteInventarioDetalle
        {
            Id = id * 10,
            AjusteInventarioId = id,
            ProductoId = productoId,
            CantidadObjetivo = cantidadObjetivo
        });
        return ajuste;
    }

    private sealed class Fixture
    {
        public Mock<IAjusteInventarioRepository> Ajustes { get; } = new();
        public Mock<IProductoRepository> Productos { get; } = new();
        public Mock<IProductoVarianteRepository> Variantes { get; } = new();
        public Mock<IMovimientoInventarioRepository> Movimientos { get; } = new();
        public Mock<IInventarioConcurrencyService> Concurrency { get; } = new();
        public Mock<IUnitOfWork> UnitOfWork { get; } = new();
        public Mock<ICurrentUserService> CurrentUser { get; } = new();
        public Mock<IAuditoriaService> Auditoria { get; } = new();

        public Fixture()
        {
            UnitOfWork
                .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(operation => operation());
            Ajustes.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
            CurrentUser.SetupGet(x => x.UsuarioId).Returns(99);
            CurrentUser.SetupGet(x => x.NombreUsuario).Returns("tester");
            Auditoria
                .Setup(x => x.RegistrarAsync(
                    It.IsAny<ModuloSistema>(),
                    It.IsAny<AccionPermiso>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<string?>(),
                    It.IsAny<object?>(),
                    It.IsAny<object?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>()))
                .Returns(Task.CompletedTask);

            Service = new AjusteInventarioService(
                Ajustes.Object,
                Productos.Object,
                Variantes.Object,
                Movimientos.Object,
                Concurrency.Object,
                UnitOfWork.Object,
                CurrentUser.Object,
                Auditoria.Object);
        }

        public AjusteInventarioService Service { get; }
    }
}
