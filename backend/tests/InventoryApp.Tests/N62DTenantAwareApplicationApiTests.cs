using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Application.Validators;
using InventoryApp.Domain.Entities;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N62DTenantAwareApplicationApiTests
{
    private static SucursalService CreateService(
        Mock<ISucursalRepository> repository,
        Mock<IAuditoriaService>? auditoria = null)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UsuarioId).Returns(7);
        currentUser.Setup(x => x.NombreUsuario).Returns("n62d-controller");
        return new SucursalService(
            repository.Object,
            currentUser.Object,
            (auditoria ?? new Mock<IAuditoriaService>()).Object);
    }

    [Fact]
    public void CreateValidator_ExigeEmpresaIdParaOwnershipTenant()
    {
        var validator = new CreateSucursalValidator();

        var result = validator.Validate(new CreateSucursalDto
        {
            EmpresaId = null,
            Codigo = "TGU-01",
            Nombre = "Centro",
            ZonaHoraria = "America/Tegucigalpa"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateSucursalDto.EmpresaId));
    }

    [Fact]
    public void UpdateValidator_ExigeEmpresaIdParaConservarOwnershipTenant()
    {
        var validator = new UpdateSucursalValidator();

        var result = validator.Validate(new UpdateSucursalDto
        {
            EmpresaId = null,
            Codigo = "TGU-01",
            Nombre = "Centro",
            ZonaHoraria = "America/Tegucigalpa"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateSucursalDto.EmpresaId));
    }

    [Fact]
    public async Task CreateAsync_SinEmpresaId_FallaCerradoAntesDePersistir()
    {
        var repository = new Mock<ISucursalRepository>();
        var service = CreateService(repository);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new CreateSucursalDto
        {
            EmpresaId = null,
            Codigo = "TGU-01",
            Nombre = "Centro",
            ZonaHoraria = "America/Tegucigalpa"
        }));

        repository.Verify(x => x.AddAsync(It.IsAny<Sucursal>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ConEmpresaId_PersisteOwnershipDeterminista()
    {
        var repository = new Mock<ISucursalRepository>();
        repository.Setup(x => x.ExisteCodigoAsync("TGU-01", null)).ReturnsAsync(false);
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        Sucursal? persisted = null;
        repository.Setup(x => x.AddAsync(It.IsAny<Sucursal>()))
            .Callback<Sucursal>(entity => persisted = entity)
            .Returns(Task.CompletedTask);

        var service = CreateService(repository);
        var result = await service.CreateAsync(new CreateSucursalDto
        {
            EmpresaId = 42,
            Codigo = "tgu-01",
            Nombre = "Centro",
            ZonaHoraria = "America/Tegucigalpa"
        });

        Assert.NotNull(persisted);
        Assert.Equal(42, persisted!.ObtenerEmpresaIdTenant());
        Assert.Equal(42, result.EmpresaId);
    }

    [Fact]
    public async Task UpdateAsync_SinEmpresaId_NoPuedeDejarOwnershipAmbiguo()
    {
        var repository = new Mock<ISucursalRepository>();
        var existing = new Sucursal
        {
            Id = 9,
            EmpresaId = 42,
            Codigo = "TGU-01",
            Nombre = "Centro",
            ZonaHoraria = "America/Tegucigalpa",
            Activa = true
        };
        repository.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(existing);

        var service = CreateService(repository);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync(9, new UpdateSucursalDto
        {
            EmpresaId = null,
            Codigo = "TGU-01",
            Nombre = "Centro editado",
            ZonaHoraria = "America/Tegucigalpa"
        }));

        Assert.Equal(42, existing.EmpresaId);
        repository.Verify(x => x.Update(It.IsAny<Sucursal>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ReadLegacyNullable_NoSeRompeDuranteRolloutPersistente()
    {
        var repository = new Mock<ISucursalRepository>();
        repository.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(new Sucursal
        {
            Id = 10,
            EmpresaId = null,
            Codigo = "LEGACY",
            Nombre = "Legacy",
            ZonaHoraria = "America/Tegucigalpa",
            Activa = true
        });
        var service = CreateService(repository);

        var result = await service.GetByIdAsync(10);

        Assert.NotNull(result);
        Assert.Null(result!.EmpresaId);
    }

    [Fact]
    public async Task BuscarAsync_EmpresaIdOpcionalSigueSiendoFiltro_NoFronteraDeAutorizacion()
    {
        var repository = new Mock<ISucursalRepository>();
        repository.Setup(x => x.BuscarAsync(null, null, 42, 1, 25))
            .ReturnsAsync((new List<Sucursal>(), 0));
        var service = CreateService(repository);

        await service.BuscarAsync(new SucursalFiltroDto
        {
            EmpresaId = 42,
            Pagina = 1,
            TamanoPagina = 25
        });

        repository.Verify(x => x.BuscarAsync(null, null, 42, 1, 25), Times.Once);
    }
}
