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

        Assert.Equal("tienda", route.Template);
        Assert.NotNull(allowAnonymous);
        AssertEndpoint(nameof(TiendaController.GetProductos), "productos");
        AssertEndpoint(nameof(TiendaController.GetProducto), "productos/{slug}");
        AssertEndpoint(nameof(TiendaController.GetCategorias), "categorias");
        AssertEndpoint(nameof(TiendaController.GetCategoria), "categorias/{slug}");
    }

    [Fact]
    public void PublicSlug_EsLegibleYResuelvePorIdEstable()
    {
        Assert.Equal("cafe-especial-14-27", PublicSlug.Create("Café Especial 14\"", 27));
        Assert.True(PublicSlug.TryGetId("nombre-anterior-27", out var id));
        Assert.Equal(27, id);
        Assert.False(PublicSlug.TryGetId("sin-id", out _));
        Assert.False(PublicSlug.TryGetId("producto-0", out _));
    }

    [Fact]
    public async Task GetProductos_ExponeSoloLaProyeccionComercialActiva()
    {
        var productos = new Mock<IProductoService>();
        productos.Setup(x => x.GetPagedAsync(It.IsAny<PagedRequest>()))
            .ReturnsAsync(new PagedResult<ProductoDto>
            {
                Items = new List<ProductoDto>
                {
                    new()
                    {
                        Id = 7,
                        Nombre = "Producto público",
                        Activo = true,
                        CategoriaId = 3,
                        CategoriaNombre = "Electrónica",
                        Precio = 1200,
                        Costo = 600,
                        Cantidad = 103,
                        FechaCreacion = new DateTime(2026, 1, 2),
                        CreadoPorNombreUsuario = "dato-reservado",
                        Variantes = new List<ProductoVarianteDto>
                        {
                            new() { Activo = true, Sku = "PUB-001", Cantidad = 3, Precio = 1200 },
                            new() { Activo = false, Sku = "NO-PUBLICO", Cantidad = 100, Precio = 1 }
                        }
                    },
                    new() { Id = 8, Nombre = "Producto inactivo", Activo = false }
                },
                Page = 1,
                PageSize = 48,
                TotalCount = 1
            });

        var controller = CrearController(productos: productos);
        var result = await controller.GetProductos(new ProductoPagedRequest { PageSize = 48 });
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<ProductoCatalogoPublicoDto>>>(ok.Value);
        var producto = Assert.Single(response.Data!.Items);

        Assert.Equal(7, producto.Id);
        Assert.Equal("producto-publico-7", producto.Slug);
        Assert.Equal(3, producto.CategoriaId);
        Assert.Equal("PUB-001", producto.Sku);
        Assert.Equal(1200, producto.Precio);
        Assert.Null(producto.PrecioOferta);
        Assert.False(producto.EsDestacado);
        Assert.Equal(3, producto.CantidadDisponible);
        Assert.True(producto.Activo);
    }

    [Fact]
    public async Task GetProducto_ResuelveSlugConIdYDevuelveSlugCanonico()
    {
        var productos = new Mock<IProductoService>();
        productos.Setup(x => x.GetByIdAsync(21)).ReturnsAsync(new ProductoDto
        {
            Id = 21,
            Nombre = "Cámara Wi-Fi",
            Activo = true,
            Cantidad = 2,
            Precio = 899
        });

        var controller = CrearController(productos: productos);
        var result = await controller.GetProducto("nombre-viejo-21");
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<ProductoCatalogoPublicoDto>>(ok.Value);

        Assert.Equal("camara-wi-fi-21", response.Data!.Slug);
        Assert.Equal(21, response.Data.Id);
    }

    [Fact]
    public async Task GetProducto_NoExponeInactivoONoValido()
    {
        var productos = new Mock<IProductoService>();
        productos.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(new ProductoDto
        {
            Id = 9,
            Nombre = "Oculto",
            Activo = false
        });
        var controller = CrearController(productos: productos);

        Assert.IsType<NotFoundObjectResult>(await controller.GetProducto("oculto-9"));
        Assert.IsType<NotFoundObjectResult>(await controller.GetProducto("slug-invalido"));
    }

    [Fact]
    public async Task GetCategorias_ExponeSoloContratoPublicoActivo()
    {
        var categorias = new Mock<ICategoriaService>();
        categorias.Setup(x => x.GetActivasAsync()).ReturnsAsync(new List<CategoriaDto>
        {
            new() { Id = 4, Nombre = "Audio y Vídeo", Descripcion = "Entretenimiento", Activa = true, TotalProductos = 8 },
            new() { Id = 5, Nombre = "Oculta", Activa = false, TotalProductos = 99 }
        });

        var controller = CrearController(categorias: categorias);
        var result = await controller.GetCategorias();
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<CategoriaCatalogoPublicoDto>>>(ok.Value);
        var categoria = Assert.Single(response.Data!);

        Assert.Equal(4, categoria.Id);
        Assert.Equal("audio-y-video-4", categoria.Slug);
        Assert.Equal(8, categoria.TotalProductos);
    }

    [Fact]
    public async Task GetCategoria_ResuelvePorSlugYBloqueaInactiva()
    {
        var categorias = new Mock<ICategoriaService>();
        categorias.Setup(x => x.GetByIdAsync(4)).ReturnsAsync(new CategoriaDto
        {
            Id = 4,
            Nombre = "Computadoras",
            Activa = true,
            TotalProductos = 5
        });
        categorias.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(new CategoriaDto
        {
            Id = 8,
            Nombre = "Interna",
            Activa = false
        });
        var controller = CrearController(categorias: categorias);

        var ok = Assert.IsType<OkObjectResult>(await controller.GetCategoria("computadoras-4"));
        var response = Assert.IsType<ApiResponse<CategoriaCatalogoPublicoDto>>(ok.Value);
        Assert.Equal("computadoras-4", response.Data!.Slug);
        Assert.IsType<NotFoundObjectResult>(await controller.GetCategoria("interna-8"));
    }

    private static TiendaController CrearController(
        Mock<IProductoService>? productos = null,
        Mock<ICategoriaService>? categorias = null) =>
        new((productos ?? new Mock<IProductoService>()).Object, (categorias ?? new Mock<ICategoriaService>()).Object);

    private static void AssertEndpoint(string metodo, string plantilla)
    {
        var endpoint = typeof(TiendaController).GetMethod(metodo);
        Assert.NotNull(endpoint);
        Assert.Equal(plantilla, endpoint!.GetCustomAttributes(typeof(HttpGetAttribute), true)
            .Cast<HttpGetAttribute>().Single().Template);
    }
}
