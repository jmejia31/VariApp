using InventoryApp.API.Controllers;
using InventoryApp.API.Filters;
using InventoryApp.Application.DTOs.Bancos;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryApp.Tests.API.Controllers;

public sealed class CuentaBancariaControllerTests
{
    private readonly Mock<ICuentaBancariaService> _serviceMock;
    private readonly CuentaBancariaController _controller;

    public CuentaBancariaControllerTests()
    {
        _serviceMock = new Mock<ICuentaBancariaService>();
        _controller = new CuentaBancariaController(_serviceMock.Object);
    }

    [Fact]
    public void Controller_ExigeAutenticacionYRutaCanonica()
    {
        var type = typeof(CuentaBancariaController);

        Assert.Single(type.GetCustomAttributes(typeof(ApiControllerAttribute), inherit: true));
        Assert.Single(type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));

        var route = Assert.Single(type.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());
        Assert.Equal("cuentas-bancarias", route.Template);
    }

    [Fact]
    public async Task GetAll_RetornaOkConLista_VerificaPermiso()
    {
        var expected = new List<CuentaBancariaDto> { new CuentaBancariaDto { Id = 1 } };
        _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(expected);

        var methodInfo = typeof(CuentaBancariaController).GetMethod(nameof(CuentaBancariaController.GetAll));
        var permisoAttr = methodInfo?.GetCustomAttributes(typeof(RequierePermisoAttribute), inherit: false)
            .Cast<RequierePermisoAttribute>()
            .FirstOrDefault();

        var result = await _controller.GetAll();

        Assert.NotNull(permisoAttr);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, okResult.Value);
        _serviceMock.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetById_CuandoNoExiste_RetornaNotFound()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((CuentaBancariaDto?)null);

        var result = await _controller.GetById(99);

        Assert.IsType<NotFoundResult>(result);
        _serviceMock.Verify(s => s.GetByIdAsync(99), Times.Once);
    }

    [Fact]
    public async Task GetById_CuandoExiste_RetornaOk()
    {
        var expected = new CuentaBancariaDto { Id = 1 };
        _serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(expected);

        var result = await _controller.GetById(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, okResult.Value);
        _serviceMock.Verify(s => s.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task Create_RetornaCreatedAtAction()
    {
        var dto = new CreateCuentaBancariaDto { Nombre = "Test" };
        var created = new CuentaBancariaDto { Id = 10, Nombre = "Test" };
        _serviceMock.Setup(s => s.AddAsync(dto)).ReturnsAsync(created);

        var result = await _controller.Create(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(CuentaBancariaController.GetById), createdResult.ActionName);
        Assert.Equal(10, createdResult.RouteValues?["id"]);
        Assert.Equal(created, createdResult.Value);
        _serviceMock.Verify(s => s.AddAsync(dto), Times.Once);
    }
}
