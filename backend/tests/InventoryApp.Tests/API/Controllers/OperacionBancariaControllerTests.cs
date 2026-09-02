using System.Security.Claims;
using InventoryApp.API.Controllers;
using InventoryApp.API.Filters;
using InventoryApp.Application.DTOs.Bancos;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryApp.Tests.API.Controllers;

public sealed class OperacionBancariaControllerTests
{
    private readonly Mock<IOperacionBancariaService> _serviceMock;
    private readonly OperacionBancariaController _controller;

    public OperacionBancariaControllerTests()
    {
        _serviceMock = new Mock<IOperacionBancariaService>();
        _controller = new OperacionBancariaController(_serviceMock.Object);
    }

    [Fact]
    public void Controller_ExigeAutenticacionYRutaCanonica()
    {
        var type = typeof(OperacionBancariaController);
        Assert.Single(type.GetCustomAttributes(typeof(ApiControllerAttribute), inherit: true));
        Assert.Single(type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
        var route = Assert.Single(type.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());
        Assert.Equal("operaciones-bancarias", route.Template);
    }

    [Theory]
    [InlineData(nameof(OperacionBancariaController.RegistrarDeposito), "deposito")]
    [InlineData(nameof(OperacionBancariaController.RegistrarRetiro), "retiro")]
    [InlineData(nameof(OperacionBancariaController.RegistrarTransferencia), "transferencia")]
    [InlineData(nameof(OperacionBancariaController.RegistrarComision), "comision")]
    [InlineData(nameof(OperacionBancariaController.RegistrarInteres), "interes")]
    [InlineData(nameof(OperacionBancariaController.RegistrarConciliacion), "conciliacion")]
    public void Acciones_TienenRutaPostYPermisoCrear(string methodName, string expectedTemplate)
    {
        var methodInfo = typeof(OperacionBancariaController).GetMethod(methodName);
        Assert.NotNull(methodInfo);

        var http = Assert.Single(methodInfo!.GetCustomAttributes(typeof(HttpPostAttribute), inherit: false).Cast<HttpPostAttribute>());
        Assert.Equal(expectedTemplate, http.Template);
        Assert.Single(methodInfo.GetCustomAttributes(typeof(RequierePermisoAttribute), inherit: false));
    }

    [Fact]
    public async Task RegistrarDeposito_SinClaimUsuario_FallaCerradoYNoInvocaServicio()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        var result = await _controller.RegistrarDeposito(new DepositoBancarioDto());

        Assert.IsType<UnauthorizedResult>(result);
        _serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegistrarDeposito_ConClaimValido_PropagaUsuarioAlServicio()
    {
        const int usuarioId = 37;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())
                }, "test"))
            }
        };
        var dto = new DepositoBancarioDto();

        var result = await _controller.RegistrarDeposito(dto);

        Assert.IsType<OkResult>(result);
        _serviceMock.Verify(s => s.RegistrarDepositoAsync(dto, usuarioId), Times.Once);
    }
}
