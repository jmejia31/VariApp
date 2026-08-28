using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities.Cajas;
using InventoryApp.Domain.Enums.Cajas;
using Moq;

namespace InventoryApp.Tests.Application.Cajas;

public sealed class CajaServiceTests
{
    [Fact]
    public async Task CrearCajaAsync_sin_usuario_autenticado_falla_cerrado()
    {
        var (service, _, _, _) = CrearServicio(autenticado: false);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.CrearCajaAsync(new CrearCajaDto { Nombre = "Caja principal" }));
    }

    [Fact]
    public async Task DesactivarCajaAsync_con_sesion_activa_rechaza_sin_mutar_estado()
    {
        var caja = new Caja("Caja principal");
        caja.Activar();
        caja.RegistrarSesionActiva(10);
        var (service, repository, unitOfWork, _) = CrearServicio();
        repository.Setup(x => x.GetCajaByIdForUpdateAsync(1)).ReturnsAsync(caja);
        PrepararTransaccion(unitOfWork);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.DesactivarCajaAsync(1));
        Assert.Equal(EstadoCaja.Activa, caja.Estado);
        Assert.Equal(10, caja.SesionActivaId);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegistrarMovimientoAsync_diferencia_durante_operaciones_rechaza_fail_closed()
    {
        var sesion = new CajaSesion(1, 7, 100m);
        sesion.IniciarOperaciones();
        var (service, repository, unitOfWork, _) = CrearServicio();
        repository.Setup(x => x.GetSesionByIdForUpdateAsync(1)).ReturnsAsync(sesion);
        PrepararTransaccion(unitOfWork);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.RegistrarMovimientoAsync(
            1,
            new RegistrarMovimientoCajaDto
            {
                Tipo = TipoMovimientoCaja.DiferenciaFaltante,
                Monto = 5m,
                Referencia = "No permitido durante operaciones"
            }));

        Assert.Empty(sesion.Movimientos);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task IniciarOperacionesAsync_usa_lectura_for_update_y_transaccion()
    {
        var sesion = new CajaSesion(1, 7, 100m);
        var (service, repository, unitOfWork, _) = CrearServicio();
        repository.Setup(x => x.GetSesionByIdForUpdateAsync(1)).ReturnsAsync(sesion);
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        repository.Setup(x => x.GetSesionByIdAsync(1, false)).ReturnsAsync(sesion);
        PrepararTransaccion(unitOfWork);

        var resultado = await service.IniciarOperacionesAsync(1);

        Assert.Equal(EstadoCajaSesion.Operaciones, resultado.Estado);
        repository.Verify(x => x.GetSesionByIdForUpdateAsync(1), Times.Once);
        unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Once);
    }

    private static (CajaService Service, Mock<ICajaRepository> Repository, Mock<IUnitOfWork> UnitOfWork, Mock<ICurrentUserService> CurrentUser)
        CrearServicio(bool autenticado = true)
    {
        var repository = new Mock<ICajaRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        currentUser.SetupGet(x => x.EstaAutenticado).Returns(autenticado);
        currentUser.SetupGet(x => x.UsuarioId).Returns(autenticado ? 7 : null);
        var service = new CajaService(repository.Object, currentUser.Object, unitOfWork.Object);
        return (service, repository, unitOfWork, currentUser);
    }

    private static void PrepararTransaccion(Mock<IUnitOfWork> unitOfWork)
    {
        unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(operation => operation());
    }
}
