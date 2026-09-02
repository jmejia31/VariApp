using InventoryApp.API.Controllers;
using InventoryApp.Application.DTOs.Bancos;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryApp.Tests.API.Controllers;

public sealed class CuentaBancariaControllerUpdateTests
{
    private readonly Mock<ICuentaBancariaService> _serviceMock;
    private readonly CuentaBancariaController _controller;

    public CuentaBancariaControllerUpdateTests()
    {
        _serviceMock = new Mock<ICuentaBancariaService>();
        _controller = new CuentaBancariaController(_serviceMock.Object);
    }

    [Fact]
    public async Task Update_RetornaNoContent_LlamaAService()
    {
        var dto = new UpdateCuentaBancariaDto { Nombre = "Nuevo Nombre" };
        var result = await _controller.Update(5, dto);
        Assert.IsType<NoContentResult>(result);
        _serviceMock.Verify(s => s.UpdateAsync(5, dto), Times.Once);
    }

    [Fact]
    public async Task Update_RetornaProblemDetails404_CuandoNoExiste()
    {
        var dto = new UpdateCuentaBancariaDto { Nombre = "Nuevo Nombre" };
        _serviceMock.Setup(s => s.UpdateAsync(99, dto)).ThrowsAsync(new InvalidOperationException());
        var result = await _controller.Update(99, dto);
        var notFound = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }
}
