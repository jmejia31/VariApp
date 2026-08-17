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
}
