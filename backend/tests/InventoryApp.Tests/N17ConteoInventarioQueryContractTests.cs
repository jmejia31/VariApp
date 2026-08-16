using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N17ConteoInventarioQueryContractTests
{
    [Fact]
    public async Task GetPaged_NormalizaPaginacionYConservaFiltrosEmpresariales()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        ConteoInventarioQueryDto? recibido = null;
        repository
            .Setup(x => x.GetPagedAsync(It.IsAny<ConteoInventarioQueryDto>()))
            .Callback<ConteoInventarioQueryDto>(query => recibido = query)
            .ReturnsAsync((new List<ConteoInventario>(), 0));
        var service = new ConteoInventarioService(
            repository.Object,
            existencias.Object,
            currentUser.Object,
            unitOfWork.Object);
        var desde = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2026, 8, 31, 23, 59, 59, DateTimeKind.Utc);
        var query = new ConteoInventarioQueryDto
        {
            Page = 0,
            PageSize = 500,
            Search = " CNT-2026 ",
            AlmacenId = 3,
            UbicacionAlmacenId = 8,
            CategoriaId = 4,
            Tipo = TipoConteoInventario.Ciclico,
            Estado = EstadoConteoInventario.EnProceso,
            Desde = desde,
            Hasta = hasta
        };

        var resultado = await service.GetPagedAsync(query);

        Assert.NotNull(recibido);
        Assert.Same(query, recibido);
        Assert.Equal(1, recibido!.Page);
        Assert.Equal(100, recibido.PageSize);
        Assert.Equal(" CNT-2026 ", recibido.Search);
        Assert.Equal(3, recibido.AlmacenId);
        Assert.Equal(8, recibido.UbicacionAlmacenId);
        Assert.Equal(4, recibido.CategoriaId);
        Assert.Equal(TipoConteoInventario.Ciclico, recibido.Tipo);
        Assert.Equal(EstadoConteoInventario.EnProceso, recibido.Estado);
        Assert.Equal(desde, recibido.Desde);
        Assert.Equal(hasta, recibido.Hasta);
        Assert.Equal(1, resultado.Page);
        Assert.Equal(100, resultado.PageSize);
        Assert.Equal(0, resultado.TotalCount);
    }

    [Fact]
    public async Task GetPaged_PageSizeNoPositivo_SeNormalizaAUno()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        repository
            .Setup(x => x.GetPagedAsync(It.IsAny<ConteoInventarioQueryDto>()))
            .ReturnsAsync((new List<ConteoInventario>(), 0));
        var service = new ConteoInventarioService(
            repository.Object,
            existencias.Object,
            currentUser.Object,
            unitOfWork.Object);
        var query = new ConteoInventarioQueryDto { Page = 2, PageSize = 0 };

        var resultado = await service.GetPagedAsync(query);

        Assert.Equal(2, query.Page);
        Assert.Equal(1, query.PageSize);
        Assert.Equal(2, resultado.Page);
        Assert.Equal(1, resultado.PageSize);
        repository.Verify(x => x.GetPagedAsync(query), Times.Once);
    }
}
