using InventoryApp.API.Controllers;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N16TransferenciasInventarioControllerBehaviorTests
{
    [Fact]
    public async Task Create_RetornaCreatedAtActionConRecursoCreado()
    {
        var service = new Mock<ITransferenciaInventarioService>();
        var dto = new CreateTransferenciaInventarioDto
        {
            AlmacenOrigenId = 1,
            AlmacenDestinoId = 2,
            Detalles = { new TransferenciaInventarioDetalleInputDto { ProductoVarianteId = 10, CantidadSolicitada = 3 } }
        };
        var creado = new TransferenciaInventarioDto { Id = 77, Numero = "TR-77", AlmacenOrigenId = 1, AlmacenDestinoId = 2 };
        service.Setup(x => x.CreateAsync(dto)).ReturnsAsync(creado);
        var controller = new TransferenciasInventarioController(service.Object);

        var result = await controller.Create(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(TransferenciasInventarioController.GetById), created.ActionName);
        Assert.Equal(77, created.RouteValues!["id"]);
        service.Verify(x => x.CreateAsync(dto), Times.Once);
    }

    [Theory]
    [InlineData("get")]
    [InlineData("solicitar")]
    public async Task AccionesSobreIdInexistente_RetornanNotFound(string accion)
    {
        var service = new Mock<ITransferenciaInventarioService>();
        service.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((TransferenciaInventarioDto?)null);
        service.Setup(x => x.SolicitarAsync(999)).ReturnsAsync((TransferenciaInventarioDto?)null);
        var controller = new TransferenciasInventarioController(service.Object);

        IActionResult result = accion == "get"
            ? await controller.GetById(999)
            : await controller.Solicitar(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Recibir_DelegaDiscrepanciasAlCasoDeUsoYRetornaOk()
    {
        var service = new Mock<ITransferenciaInventarioService>();
        var dto = new RecibirTransferenciaInventarioDto
        {
            Detalles =
            {
                new RecibirTransferenciaInventarioDetalleDto
                {
                    DetalleId = 5,
                    CantidadRecibida = 7,
                    CantidadFaltante = 1,
                    CantidadDanada = 1,
                    CantidadSobrante = 0
                }
            }
        };
        var recibida = new TransferenciaInventarioDto { Id = 21, Estado = "Recibida" };
        service.Setup(x => x.RecibirAsync(21, dto)).ReturnsAsync(recibida);
        var controller = new TransferenciasInventarioController(service.Object);

        var result = await controller.Recibir(21, dto);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(x => x.RecibirAsync(21, dto), Times.Once);
    }
}
