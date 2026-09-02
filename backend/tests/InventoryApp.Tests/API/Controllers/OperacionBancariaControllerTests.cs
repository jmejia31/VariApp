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

    [Theory]
    [InlineData(nameof(OperacionBancariaController.RegistrarDeposito), typeof(DepositoBancariaDto))]
    public void Placeholder_DoNotUse() { }
}
