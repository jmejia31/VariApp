using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N17ConteoInventarioAjusteTests
{
    [Fact]
    public async Task GenerarAjuste_ConteoAprobado_CreaBorradorYVinculaDiferencias()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = CrearUnitOfWork();
        var ajustes = new Mock<IAjusteInventarioService>();
        var conteo = CrearAprobadoConDiferencia();
        repository.Setup(x => x.GetByIdForUpdateAsync(10)).ReturnsAsync(conteo);
        ajustes.Setup(x => x.CreateAsync(It.IsAny<CreateAjusteInventarioDto>()))
            .ReturnsAsync(new AjusteInventarioDto { Id = 55, NumeroAjuste = "AI-000055" });
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object, ajustes.Object);

        var result = await service.GenerarAjusteAsync(10);

        Assert.NotNull(result);
        Assert.Equal(55, result!.Id);
        Assert.Equal(55, conteo.Detalles.Single().AjusteInventarioId);
        ajustes.Verify(x => x.CreateAsync(It.Is<CreateAjusteInventarioDto>(dto =>
            dto.Detalles.Count == 1 &&
            dto.Detalles[0].ProductoId == 21 &&
            dto.Detalles[0].ProductoVarianteId == 9 &&
            dto.Detalles[0].AlmacenId == 3 &&
            dto.Detalles[0].CantidadObjetivo == 6)), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GenerarAjuste_Reintento_RetornaAjusteExistenteSinDuplicarlo()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = CrearUnitOfWork();
        var ajustes = new Mock<IAjusteInventarioService>();
        var conteo = CrearAprobadoConDiferencia();
        conteo.Detalles.Single().VincularAjuste(55);
        repository.Setup(x => x.GetByIdForUpdateAsync(10)).ReturnsAsync(conteo);
        ajustes.Setup(x => x.GetByIdAsync(55)).ReturnsAsync(new AjusteInventarioDto { Id = 55, NumeroAjuste = "AI-000055" });
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object, ajustes.Object);

        var result = await service.GenerarAjusteAsync(10);

        Assert.NotNull(result);
        Assert.Equal(55, result!.Id);
        ajustes.Verify(x => x.CreateAsync(It.IsAny<CreateAjusteInventarioDto>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GenerarAjuste_SinDiferencias_FallaCerradoSinCrearNiPersistir()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = CrearUnitOfWork();
        var ajustes = new Mock<IAjusteInventarioService>();
        var conteo = CrearAprobadoConDiferencia(cantidadContada: 8);
        repository.Setup(x => x.GetByIdForUpdateAsync(10)).ReturnsAsync(conteo);
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object, ajustes.Object);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.GenerarAjusteAsync(10));

        Assert.Contains("no contiene diferencias", error.Message, StringComparison.OrdinalIgnoreCase);
        ajustes.Verify(x => x.CreateAsync(It.IsAny<CreateAjusteInventarioDto>()), Times.Never);
        repository.Verify(x => x.Update(It.IsAny<ConteoInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    private static Mock<IUnitOfWork> CrearUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(operation => operation());
        return unitOfWork;
    }

    private static ConteoInventario CrearAprobadoConDiferencia(int cantidadContada = 6)
    {
        var variante = new ProductoVariante { Id = 9, ProductoId = 21, Sku = "SKU-21-9" };
        var detalle = new ConteoInventarioDetalle
        {
            Id = 4,
            ProductoVarianteId = 9,
            ProductoVariante = variante,
            AlmacenId = 3
        };
        detalle.MaterializarSnapshot(8);
        var conteo = new ConteoInventario
        {
            Id = 10,
            Numero = "CNT-10",
            Tipo = TipoConteoInventario.General,
            AlmacenId = 3,
            Detalles = new List<ConteoInventarioDetalle> { detalle }
        };
        conteo.Iniciar(7, DateTime.UtcNow.AddMinutes(-3));
        detalle.RegistrarConteo(cantidadContada, 7, DateTime.UtcNow.AddMinutes(-2));
        conteo.Cerrar(7, DateTime.UtcNow.AddMinutes(-1));
        conteo.Aprobar(7, DateTime.UtcNow);
        return conteo;
    }
}
