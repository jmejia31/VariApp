using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N17ConteoInventarioServiceTests
{
    [Fact]
    public async Task GetById_ConteoCiegoEnProceso_NoExponeStockEsperado()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var conteo = CrearConteoCiegoEnProceso();
        repository.Setup(x => x.GetByIdAsync(18)).ReturnsAsync(conteo);
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object);

        var dto = await service.GetByIdAsync(18);

        Assert.NotNull(dto);
        Assert.True(dto!.EsCiego);
        Assert.Equal(EstadoConteoInventario.EnProceso, dto.Estado);
        Assert.Single(dto.Detalles);
        Assert.Null(dto.Detalles[0].StockEsperado);
    }

    [Fact]
    public async Task GetPaged_NormalizaLimitesAntesDeConsultarRepositorio()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        repository.Setup(x => x.GetPagedAsync(It.IsAny<ConteoInventarioQueryDto>()))
            .ReturnsAsync((new List<ConteoInventario>(), 0));
        var service = new ConteoInventarioService(repository.Object, existencias.Object, currentUser.Object, unitOfWork.Object);
        var query = new ConteoInventarioQueryDto { Page = 0, PageSize = 999 };

        var page = await service.GetPagedAsync(query);

        Assert.Equal(1, query.Page);
        Assert.Equal(100, query.PageSize);
        Assert.Equal(1, page.Page);
        Assert.Equal(100, page.PageSize);
        repository.Verify(x => x.GetPagedAsync(It.Is<ConteoInventarioQueryDto>(q => q.Page == 1 && q.PageSize == 100)), Times.Once);
    }

    private static ConteoInventario CrearConteoCiegoEnProceso()
    {
        var detalle = new ConteoInventarioDetalle
        {
            ProductoVarianteId = 9,
            AlmacenId = 3
        };
        detalle.MaterializarSnapshot(14);
        var conteo = new ConteoInventario
        {
            Id = 18,
            Numero = "CNT-CIEGO-18",
            Tipo = TipoConteoInventario.Ciego,
            AlmacenId = 3,
            EsCiego = true,
            Detalles = new List<ConteoInventarioDetalle> { detalle }
        };
        conteo.Iniciar(7, DateTime.UtcNow);
        return conteo;
    }
}
