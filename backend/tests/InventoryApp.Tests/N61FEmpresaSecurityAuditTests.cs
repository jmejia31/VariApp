using InventoryApp.API.Controllers;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N61FEmpresaSecurityAuditTests
{
    private readonly Mock<IEmpresaRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();

    public N61FEmpresaSecurityAuditTests()
    {
        _currentUser.SetupGet(x => x.UsuarioId).Returns(41);
        _currentUser.SetupGet(x => x.NombreUsuario).Returns("vaep-n61f");
        _auditoria
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
    }

    [Fact]
    public void EmpresasController_UsesCanonicalServiceInjection()
    {
        var constructor = Assert.Single(typeof(EmpresasController).GetConstructors());
        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal(typeof(IEmpresaService), parameter.ParameterType);
    }

    [Fact]
    public async Task CreateAsync_AuditsSuccessfulEmpresaMutation()
    {
        _repository
            .Setup(x => x.AddAsync(It.IsAny<Empresa>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmpresaService(_repository.Object, _currentUser.Object, _auditoria.Object);
        await service.CreateAsync(new CreateEmpresaDto { Nombre = "Empresa segura" });

        _auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Crear,
            It.Is<string>(value => value.Contains("Empresa creada")),
            It.IsAny<int?>(),
            "Empresa",
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Theory]
    [InlineData(true, AccionPermiso.Activar)]
    [InlineData(false, AccionPermiso.Desactivar)]
    public async Task CambiarEstadoAsync_AuditsTheExactStatePermission(bool targetState, AccionPermiso expectedAction)
    {
        var empresa = new Empresa("Empresa auditada");
        if (!targetState)
            empresa.Activar();
        else
            empresa.Desactivar();

        _repository
            .Setup(x => x.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empresa);
        _repository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmpresaService(_repository.Object, _currentUser.Object, _auditoria.Object);
        await service.CambiarEstadoAsync(7, targetState);

        _auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.Configuracion,
            expectedAction,
            It.IsAny<string>(),
            It.IsAny<int?>(),
            "Empresa",
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task SaveFailure_DoesNotEmitFalseAuditSuccess()
    {
        _repository
            .Setup(x => x.AddAsync(It.IsAny<Empresa>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new EmpresaService(_repository.Object, _currentUser.Object, _auditoria.Object);

        await Assert.ThrowsAsync<InventoryApp.Application.Exceptions.BusinessRuleException>(() =>
            service.CreateAsync(new CreateEmpresaDto { Nombre = "No persistida" }));

        _auditoria.Verify(x => x.RegistrarAsync(
            It.IsAny<ModuloSistema>(),
            It.IsAny<AccionPermiso>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string?>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Never);
    }
}
