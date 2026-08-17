using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N18ReservaInventarioStockReservadoTests
{
    [Fact]
    public async Task Activar_ReservaStockSobreExistenciaAutoritativa_YNoMutaStockFisico()
    {
        var reserva = CrearBorrador(cantidad: 3);
        var existencia = new ExistenciaVariante
        {
            Id = 91,
            ProductoVarianteId = 10,
            AlmacenId = 20,
            StockFisico = 12,
            StockReservado = 2,
            StockTransito = 0,
            ProductoVariante = reserva.Detalles.Single().ProductoVariante
        };
        var clave = new InventarioExistenciaClave(10, 20, null);

        var repository = new Mock<IReservaInventarioRepository>();
        repository.Setup(x => x.GetByIdAsync(7, It.IsAny<bool>())).ReturnsAsync(reserva);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var existencias = new Mock<IExistenciaVarianteConcurrencyService>();
        existencias.Setup(x => x.BloquearYValidarExistenciasAsync(
                It.IsAny<IEnumerable<InventarioDemandaExistencia>>(), true))
            .ReturnsAsync(new InventarioExistenciaLockSet(
                new Dictionary<InventarioExistenciaClave, ExistenciaVariante> { [clave] = existencia },
                new[] { new InventarioDemandaExistencia(1, 10, 20, null, 3) }));
        existencias.Setup(x => x.AjustarStockReservadoPesimistaAsync(clave, 2, 5))
            .Returns(Task.CompletedTask)
            .Callback(() => existencia.EstablecerStocks(
                existencia.StockFisico, 5, existencia.StockTransito,
                existencia.StockMinimo, existencia.StockMaximo));

        var service = CrearServicio(repository, existencias);
        var resultado = await service.ActivarAsync(7);

        Assert.Equal(EstadoReservaInventario.Activa.ToString(), resultado.Estado);
        Assert.Equal(12, existencia.StockFisico);
        Assert.Equal(5, existencia.StockReservado);
        existencias.Verify(x => x.AjustarStockReservadoPesimistaAsync(clave, 2, 5), Times.Once);
    }

    [Fact]
    public async Task Liberar_ReservaActiva_RetiraExactamenteStockReservado_YPreservaFisico()
    {
        var reserva = CrearBorrador(cantidad: 3);
        reserva.Activar(usuarioId: 5, fecha: DateTime.UtcNow);
        var existencia = new ExistenciaVariante
        {
            Id = 91,
            ProductoVarianteId = 10,
            AlmacenId = 20,
            StockFisico = 12,
            StockReservado = 5,
            StockTransito = 0,
            ProductoVariante = reserva.Detalles.Single().ProductoVariante
        };
        var clave = new InventarioExistenciaClave(10, 20, null);

        var repository = new Mock<IReservaInventarioRepository>();
        repository.Setup(x => x.GetByIdAsync(7, It.IsAny<bool>())).ReturnsAsync(reserva);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var existencias = new Mock<IExistenciaVarianteConcurrencyService>();
        existencias.Setup(x => x.BloquearYValidarExistenciasAsync(
                It.IsAny<IEnumerable<InventarioDemandaExistencia>>(), false))
            .ReturnsAsync(new InventarioExistenciaLockSet(
                new Dictionary<InventarioExistenciaClave, ExistenciaVariante> { [clave] = existencia },
                new[] { new InventarioDemandaExistencia(1, 10, 20, null, 3) }));
        existencias.Setup(x => x.AjustarStockReservadoPesimistaAsync(clave, 5, 2))
            .Returns(Task.CompletedTask)
            .Callback(() => existencia.EstablecerStocks(
                existencia.StockFisico, 2, existencia.StockTransito,
                existencia.StockMinimo, existencia.StockMaximo));

        var service = CrearServicio(repository, existencias);
        var resultado = await service.LiberarAsync(7, new() { Motivo = "Cliente desistió" });

        Assert.Equal(EstadoReservaInventario.Liberada.ToString(), resultado.Estado);
        Assert.Equal(12, existencia.StockFisico);
        Assert.Equal(2, existencia.StockReservado);
        existencias.Verify(x => x.AjustarStockReservadoPesimistaAsync(clave, 5, 2), Times.Once);
    }

    private static ReservaInventarioService CrearServicio(
        Mock<IReservaInventarioRepository> repository,
        Mock<IExistenciaVarianteConcurrencyService> existencias)
    {
        var variantes = new Mock<IProductoVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.EstaAutenticado).Returns(true);
        currentUser.SetupGet(x => x.UsuarioId).Returns(5);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("qa-reservas");

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> action) => action());

        return new ReservaInventarioService(
            repository.Object,
            variantes.Object,
            existencias.Object,
            currentUser.Object,
            unitOfWork.Object);
    }

    private static ReservaInventario CrearBorrador(int cantidad)
    {
        var variante = new ProductoVariante
        {
            Id = 10,
            ProductoId = 1,
            Activo = true,
            Sku = "SKU-RSV",
            Producto = new Producto { Id = 1, Nombre = "Producto reserva" }
        };
        var detalle = new ReservaInventarioDetalle
        {
            Id = 8,
            ProductoVarianteId = variante.Id,
            ProductoVariante = variante,
            AlmacenId = 20,
            ProductoSkuSnapshot = variante.Sku
        };
        detalle.EstablecerCantidadReservada(cantidad);

        return new ReservaInventario
        {
            Id = 7,
            Numero = "RSV-TEST-0007",
            CreadoPorUsuarioId = 5,
            Detalles = new List<ReservaInventarioDetalle> { detalle }
        };
    }
}
