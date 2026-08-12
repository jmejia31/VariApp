using InventoryApp.API.Controllers;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class TiendaPublicaTests
{
    [Fact]
    public void Controller_EsIndependienteYPermiteConsultaAnonima()
    {
        var route = typeof(TiendaController).GetCustomAttributes(typeof(RouteAttribute), true)
            .Cast<RouteAttribute>().Single();
        var allowAnonymous = typeof(TiendaController)
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), true).SingleOrDefault();
        var endpoint = typeof(TiendaController).GetMethod(nameof(TiendaController.GetProductos));

        Assert.Equal("tienda", route.Template);
        Assert.NotNull(allowAnonymous);
        Assert.NotNull(endpoint);
        Assert.Equal("productos", endpoint!.GetCustomAttributes(typeof(HttpGetAttribute), true)
            .Cast<HttpGetAttribute>().Single().Template);
    }

    [Fact]
    public async Task GetProductos_ExponeSoloLaProyeccionComercialActiva()
    {
        var servicio = new Mock<IProductoService>();
        servicio.Setup(x => x.GetPagedAsync(It.IsAny<PagedRequest>()))
            .ReturnsAsync(new PagedResult<ProductoDto>
            {
                Items = new List<ProductoDto>
                {
                    new()
                    {
                        Id = 7,
                        Nombre = "Producto público",
                        Activo = true,
                        Precio = 1200,
                        Costo = 600,
                        Cantidad = 3,
                        CreadoPorNombreUsuario = "dato-reservado"
                    },
                    new() { Id = 8, Nombre = "Producto inactivo", Activo = false }
                },
                Page = 1,
                PageSize = 48,
                TotalCount = 1
            });

        var controller = new TiendaController(servicio.Object);
        var result = await controller.GetProductos(new ProductoPagedRequest { PageSize = 48 });
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<ProductoCatalogoPublicoDto>>>(ok.Value);
        var producto = Assert.Single(response.Data!.Items);

        Assert.Equal(7, producto.Id);
        Assert.Equal(1200, producto.Precio);
        Assert.Equal(3, producto.CantidadDisponible);
    }
}
