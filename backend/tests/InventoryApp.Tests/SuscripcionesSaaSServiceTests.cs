using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class SuscripcionesSaaSServiceTests
{
    [Fact]
    public async Task Onboarding_TenantNoVerificado_FallaCerradoSinTocarRepositorio()
    {
        var repository = new Mock<ISuscripcionesSaaSRepository>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(x => x.ObtenerActualAsync(27, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);
        var service = new SuscripcionesSaaSService(repository.Object, scope.Object);

        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.OnboardingAsync(
            27,
            new OnboardingSuscripcionSaaSRequest("BASIC", DateTime.UtcNow),
            "req-tenant-denied",
            CancellationToken.None));

        Assert.Contains(SuscripcionSaaSErrorCodes.TenantNoAutorizado, ex.Message);
        repository.VerifyNoOtherCalls();
        scope.VerifyAll();
    }

    [Fact]
    public async Task Onboarding_ReplayIdempotente_DevuelveMismaSuscripcionSinDuplicar()
    {
        const int empresaId = 9;
        const int planId = 4;
        var inicio = new DateTime(2026, 9, 12, 23, 0, 0, DateTimeKind.Utc);
        var plan = new Plan("BASIC", "Basic") { Id = planId };
        var existente = new Suscripcion(empresaId, planId, inicio) { Id = 88 };
        var repository = new Mock<ISuscripcionesSaaSRepository>(MockBehavior.Strict);
        var scope = CrearScope(empresaId);

        repository.Setup(x => x.ObtenerPorIdempotenciaAsync(empresaId, "req-replay-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);
        repository.Setup(x => x.ObtenerPlanPorIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var service = new SuscripcionesSaaSService(repository.Object, scope.Object);
        var dto = await service.OnboardingAsync(
            empresaId,
            new OnboardingSuscripcionSaaSRequest(" basic ", inicio),
            " req-replay-1 ",
            CancellationToken.None);

        Assert.Equal(existente.Id, dto.Id);
        Assert.Equal("BASIC", dto.PlanCodigo);
        repository.Verify(x => x.AgregarAsync(It.IsAny<Suscripcion>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
        repository.VerifyAll();
        scope.VerifyAll();
    }

    [Fact]
    public async Task Onboarding_MismaKeyConPayloadDistinto_RechazaConflicto()
    {
        const int empresaId = 12;
        var existente = new Suscripcion(empresaId, 7, new DateTime(2026, 9, 12, 20, 0, 0, DateTimeKind.Utc)) { Id = 91 };
        var plan = new Plan("PRO", "Pro") { Id = 7 };
        var repository = new Mock<ISuscripcionesSaaSRepository>(MockBehavior.Strict);
        var scope = CrearScope(empresaId);

        repository.Setup(x => x.ObtenerPorIdempotenciaAsync(empresaId, "req-conflict", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);
        repository.Setup(x => x.ObtenerPlanPorIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var service = new SuscripcionesSaaSService(repository.Object, scope.Object);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.OnboardingAsync(
            empresaId,
            new OnboardingSuscripcionSaaSRequest("BASIC", existente.InicioUtc),
            "req-conflict",
            CancellationToken.None));

        Assert.Contains(SuscripcionSaaSErrorCodes.IdempotencyKeyConflictiva, ex.Message);
        repository.Verify(x => x.AgregarAsync(It.IsAny<Suscripcion>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.VerifyAll();
        scope.VerifyAll();
    }

    [Fact]
    public async Task Onboarding_Nuevo_PersisteConTenantServerVerified()
    {
        const int empresaId = 31;
        const int planId = 6;
        var inicio = new DateTime(2026, 9, 13, 0, 0, 0, DateTimeKind.Utc);
        var plan = new Plan("TEAM", "Team") { Id = planId };
        var repository = new Mock<ISuscripcionesSaaSRepository>(MockBehavior.Strict);
        var scope = CrearScope(empresaId);
        Suscripcion? capturada = null;

        repository.Setup(x => x.ObtenerPorIdempotenciaAsync(empresaId, "req-new", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Suscripcion?)null);
        repository.Setup(x => x.ObtenerVigenteAsync(empresaId, inicio, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Suscripcion?)null);
        repository.Setup(x => x.ObtenerPlanActivoPorCodigoAsync("TEAM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        repository.Setup(x => x.AgregarAsync(It.IsAny<Suscripcion>(), "req-new", It.IsAny<CancellationToken>()))
            .Callback<Suscripcion, string, CancellationToken>((suscripcion, _, _) => capturada = suscripcion)
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new SuscripcionesSaaSService(repository.Object, scope.Object);
        var dto = await service.OnboardingAsync(
            empresaId,
            new OnboardingSuscripcionSaaSRequest("team", inicio),
            "req-new",
            CancellationToken.None);

        Assert.NotNull(capturada);
        Assert.Equal(empresaId, capturada!.EmpresaId);
        Assert.Equal(planId, capturada.PlanId);
        Assert.Equal("TEAM", dto.PlanCodigo);
        repository.VerifyAll();
        scope.VerifyAll();
    }

    private static Mock<IUsuarioScopeService> CrearScope(int empresaId)
    {
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(x => x.ObtenerActualAsync(empresaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(
                UsuarioId: 100,
                EmpresaId: empresaId,
                RolId: 1,
                RolNombre: "ADMIN",
                EsAdministrador: true));
        return scope;
    }
}
