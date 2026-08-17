using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N17ConteoInventarioBlindPagedDraftSecurityTests
{
    [Fact]
    public async Task GetPaged_ConteoCiegoEnBorrador_NoExponeStockEsperadoNiDiferencias()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var detalle = new ConteoInventarioDetalle
        {
            Id = 4,
            ProductoVarianteId = 9,
            AlmacenId = 3
        };
        detalle.MaterializarSnapshot(14);

        var conteo = new ConteoInventario
        {
            Id = 18,
            Numero = "CNT-CIEGO-BORRADOR-18",
            Tipo = TipoConteoInventario.Ciego,
            AlmacenId = 3,
            EsCiego = true,
            Detalles = new List<ConteoInventarioDetalle> { detalle }
        };

        repository.Setup(x => x.GetPagedAsync(It.IsAny<ConteoInventarioQueryDto>()))
            .ReturnsAsync((new List<ConteoInventario> { conteo }, 1));

        var service = new ConteoInventarioService(
            repository.Object,
            existencias.Object,
            currentUser.Object,
            unitOfWork.Object);

        var pagina = await service.GetPagedAsync(new ConteoInventarioQueryDto());

        var dto = Assert.Single(pagina.Items);
        var linea = Assert.Single(dto.Detalles);
        Assert.True(dto.EsCiego);
        Assert.Equal(EstadoConteoInventario.Borrador, dto.Estado);
        Assert.Null(linea.StockEsperado);
        Assert.Null(linea.Diferencia);
        Assert.Equal(0, dto.CantidadConDiferencia);
        Assert.Equal(0, dto.DiferenciaNeta);
    }

    [Fact]
    public async Task GetPaged_ConteoCiegoCanceladoAntesDeCierre_MantieneReferenciaOculta()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var detalle = new ConteoInventarioDetalle
        {
            Id = 5,
            ProductoVarianteId = 10,
            AlmacenId = 4
        };
        detalle.MaterializarSnapshot(21);

        var conteo = new ConteoInventario
        {
            Id = 19,
            Numero = "CNT-CIEGO-CANCELADO-19",
            Tipo = TipoConteoInventario.Ciego,
            AlmacenId = 4,
            EsCiego = true,
            Detalles = new List<ConteoInventarioDetalle> { detalle }
        };
        conteo.Cancelar(77, "Cancelado antes del cierre", DateTime.UtcNow);

        repository.Setup(x => x.GetPagedAsync(It.IsAny<ConteoInventarioQueryDto>()))
            .ReturnsAsync((new List<ConteoInventario> { conteo }, 1));

        var service = new ConteoInventarioService(
            repository.Object,
            existencias.Object,
            currentUser.Object,
            unitOfWork.Object);

        var pagina = await service.GetPagedAsync(new ConteoInventarioQueryDto());

        var dto = Assert.Single(pagina.Items);
        var linea = Assert.Single(dto.Detalles);
        Assert.True(dto.EsCiego);
        Assert.Equal(EstadoConteoInventario.Cancelado, dto.Estado);
        Assert.Null(dto.FechaCierre);
        Assert.Null(linea.StockEsperado);
        Assert.Null(linea.Diferencia);
        Assert.Equal(0, dto.CantidadConDiferencia);
        Assert.Equal(0, dto.DiferenciaNeta);
    }

    [Fact]
    public async Task GetById_ConteoCiegoCanceladoAntesDeCierre_MantieneReferenciaOculta()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var detalle = new ConteoInventarioDetalle
        {
            Id = 6,
            ProductoVarianteId = 11,
            AlmacenId = 5
        };
        detalle.MaterializarSnapshot(34);
        detalle.RegistrarConteo(29, 77, DateTime.UtcNow);

        var conteo = new ConteoInventario
        {
            Id = 20,
            Numero = "CNT-CIEGO-CANCELADO-20",
            Tipo = TipoConteoInventario.Ciego,
            AlmacenId = 5,
            EsCiego = true,
            Detalles = new List<ConteoInventarioDetalle> { detalle }
        };
        conteo.Cancelar(77, "Cancelado antes de cerrar", DateTime.UtcNow);

        repository.Setup(x => x.GetByIdAsync(20)).ReturnsAsync(conteo);

        var service = new ConteoInventarioService(
            repository.Object,
            existencias.Object,
            currentUser.Object,
            unitOfWork.Object);

        var dto = await service.GetByIdAsync(20);

        Assert.NotNull(dto);
        var linea = Assert.Single(dto!.Detalles);
        Assert.True(dto.EsCiego);
        Assert.Equal(EstadoConteoInventario.Cancelado, dto.Estado);
        Assert.Null(dto.FechaCierre);
        Assert.Equal(29, linea.CantidadContada);
        Assert.Null(linea.StockEsperado);
        Assert.Null(linea.Diferencia);
        Assert.Equal(0, dto.CantidadConDiferencia);
        Assert.Equal(0, dto.DiferenciaNeta);
    }
}
