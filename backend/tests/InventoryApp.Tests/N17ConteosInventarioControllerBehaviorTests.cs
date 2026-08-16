using InventoryApp.API.Controllers;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N17ConteosInventarioControllerBehaviorTests
{
    [Fact]
    public async Task Create_RetornaCreatedAtActionConConteoCreado()
    {
        var service = new Mock<IConteoInventarioService>();
        var dto = new CreateConteoInventarioDto { AlmacenId = 1, ProductoVarianteIds = { 10 } };
        var creado = new ConteoInventarioDto { Id = 77, Numero = "CNT-77", AlmacenId = 1 };
        service.Setup(x => x.CreateAsync(dto)).ReturnsAsync(creado);
        var controller = new ConteosInventarioController(service.Object);

        var result = await controller.Create(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ConteosInventarioController.GetById), created.ActionName);
        Assert.Equal(77, created.RouteValues!["id"]);
        service.Verify(x => x.CreateAsync(dto), Times.Once);
    }

    [Fact]
    public async Task Buscar_PropagaPaginacionTipada()
    {
        var service = new Mock<IConteoInventarioService>();
        var query = new ConteoInventarioQueryDto { Page = 2, PageSize = 15, Search = "CNT" };
        var page = new PagedResult<ConteoInventarioDto> { Page = 2, PageSize = 15, TotalCount = 31 };
        service.Setup(x => x.GetPagedAsync(query)).ReturnsAsync(page);
        var controller = new ConteosInventarioController(service.Object);

        var result = await controller.Buscar(query);

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<PagedResult<ConteoInventarioDto>>>(ok.Value);
        Assert.Same(page, envelope.Data);
        service.Verify(x => x.GetPagedAsync(query), Times.Once);
    }

    [Theory]
    [InlineData("get")]
    [InlineData("iniciar")]
    [InlineData("cerrar")]
    [InlineData("aprobar")]
    public async Task AccionesSobreConteoInexistente_RetornanNotFound(string accion)
    {
        var service = new Mock<IConteoInventarioService>();
        service.Setup(x => x.GetByIdAsync(404)).ReturnsAsync((ConteoInventarioDto?)null);
        service.Setup(x => x.IniciarAsync(404)).ReturnsAsync((ConteoInventarioDto?)null);
        service.Setup(x => x.CerrarAsync(404)).ReturnsAsync((ConteoInventarioDto?)null);
        service.Setup(x => x.AprobarAsync(404)).ReturnsAsync((ConteoInventarioDto?)null);
        var controller = new ConteosInventarioController(service.Object);

        IActionResult result = accion switch
        {
            "get" => await controller.GetById(404),
            "iniciar" => await controller.Iniciar(404),
            "cerrar" => await controller.Cerrar(404),
            _ => await controller.Aprobar(404)
        };

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Capturar_ReglaDeNegocio_PropagaExcepcionParaProblemDetailsGlobal()
    {
        var service = new Mock<IConteoInventarioService>();
        var dto = new CapturarConteoInventarioDetalleDto { CantidadContada = 3 };
        service.Setup(x => x.CapturarDetalleAsync(12, 4, dto))
            .ThrowsAsync(new BusinessRuleException("Solo un conteo en proceso admite capturas."));
        var controller = new ConteosInventarioController(service.Object);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => controller.Capturar(12, 4, dto));

        Assert.Contains("proceso", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cancelar_DelegaMotivoYRetornaOk()
    {
        var service = new Mock<IConteoInventarioService>();
        var dto = new CancelarConteoInventarioDto { Motivo = "Reconteo requerido" };
        var cancelado = new ConteoInventarioDto { Id = 20, Numero = "CNT-20" };
        service.Setup(x => x.CancelarAsync(20, dto.Motivo)).ReturnsAsync(cancelado);
        var controller = new ConteosInventarioController(service.Object);

        var result = await controller.Cancelar(20, dto);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(x => x.CancelarAsync(20, dto.Motivo), Times.Once);
    }
}
