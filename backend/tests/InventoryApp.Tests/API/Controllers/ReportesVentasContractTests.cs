using System.Reflection;
using InventoryApp.API.Controllers;
using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InventoryApp.Tests.API.Controllers;

public sealed class ReportesVentasContractTests
{
    [Fact]
    public void Controller_ExigeAutenticacionRutaYPermisoVentasVer()
    {
        var type = typeof(ReportesVentasController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("ventas/reportes", type.GetCustomAttribute<RouteAttribute>()?.Template);

        foreach (var methodName in new[] { nameof(ReportesVentasController.GetResumen), nameof(ReportesVentasController.GetDetalle) })
        {
            var method = type.GetMethod(methodName);
            Assert.NotNull(method);
            var permiso = Assert.Single(method!.CustomAttributes.Where(a => a.AttributeType == typeof(RequierePermisoAttribute)));
            Assert.Equal((int)ModuloSistema.Ventas, Convert.ToInt32(permiso.ConstructorArguments[0].Value));
            Assert.Equal((int)AccionPermiso.Ver, Convert.ToInt32(permiso.ConstructorArguments[1].Value));
        }
    }

    [Fact]
    public async Task Resumen_RechazaRangoInvertidoAntesDeConsultarServicio()
    {
        var service = new Mock<IReporteVentasService>(MockBehavior.Strict);
        var controller = new ReportesVentasController(service.Object);
        var filtro = new ReporteVentasFiltroDto
        {
            Desde = new DateTime(2026, 9, 10),
            Hasta = new DateTime(2026, 9, 1)
        };

        var result = await controller.GetResumen(filtro);

        Assert.IsType<BadRequestObjectResult>(result);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Resumen_NoAdministradorNoPuedeForjarVendedorIdParaExpandirAlcance()
    {
        await using var context = CreateContext();
        context.Ventas.AddRange(
            Confirmada("OWN", 7, 100m),
            Confirmada("FORGED", 99, 900m));
        await context.SaveChangesAsync();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(7, 2, "Operador", EsAdministrador: false));
        var service = new ReporteVentasService(context, scope.Object);

        var result = await service.ObtenerResumenAsync(new ReporteVentasFiltroDto { VendedorId = 99 });

        Assert.Equal(100m, result.Total);
        Assert.Equal(100m, result.Subtotal);
    }

    [Fact]
    public async Task Detalle_FiltraSucursalPorAlmacenYPreservaSnapshotsHistoricosNulos()
    {
        await using var context = CreateContext();
        var venta = Confirmada("V-001", 7, 30m);
        var almacen1 = new Almacen { Id = 11, SucursalId = 1, Codigo = "A-1", Nombre = "Almacen 1" };
        var almacen2 = new Almacen { Id = 22, SucursalId = 2, Codigo = "A-2", Nombre = "Almacen 2" };
        context.AddRange(venta, almacen1, almacen2);
        await context.SaveChangesAsync();

        context.VentaDetalles.AddRange(
            new VentaDetalle
            {
                VentaId = venta.Id,
                Venta = venta,
                ProductoId = 101,
                AlmacenId = almacen1.Id,
                Almacen = almacen1,
                Cantidad = 1,
                PrecioUnitario = 10m,
                CostoUnitarioSnapshot = 5m,
                Subtotal = 10m,
                UtilidadBruta = 5m,
                ProductoNombreSnapshot = "Producto historico sucursal 1",
                ProductoMarcaSnapshot = "Marca historica 1",
                ProductoModeloSnapshot = "Modelo historico 1"
            },
            new VentaDetalle
            {
                VentaId = venta.Id,
                Venta = venta,
                ProductoId = 202,
                AlmacenId = almacen2.Id,
                Almacen = almacen2,
                Cantidad = 1,
                PrecioUnitario = 20m,
                CostoUnitarioSnapshot = 8m,
                Subtotal = 20m,
                UtilidadBruta = 12m,
                ProductoNombreSnapshot = "Producto historico sucursal 2",
                ProductoMarcaSnapshot = "Marca historica 2",
                ProductoModeloSnapshot = "Modelo historico 2",
                ProductoColorSnapshot = null,
                ProductoTallaSnapshot = null,
                ProductoSkuSnapshot = null
            });
        await context.SaveChangesAsync();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Administrador", EsAdministrador: true));
        var service = new ReporteVentasService(context, scope.Object);

        var result = await service.ObtenerDetallePaginadoAsync(new ReporteVentasFiltroDto
        {
            SucursalId = 2,
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(1, result.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.Equal(2, item.SucursalId);
        Assert.Equal("Producto historico sucursal 2", item.ProductoNombre);
        Assert.Equal("Marca historica 2", item.ProductoMarca);
        Assert.Equal("Modelo historico 2", item.ProductoModelo);
        Assert.Null(item.ProductoColor);
        Assert.Null(item.ProductoTalla);
        Assert.Null(item.ProductoSku);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static Venta Confirmada(string numero, int usuarioId, decimal total) => new()
    {
        NumeroVenta = numero,
        Fecha = new DateTime(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc),
        Estado = EstadoDocumento.Confirmada,
        CreadoPorUsuarioId = usuarioId,
        CreadoPorNombreUsuario = $"U{usuarioId}",
        ImporteBruto = total,
        ImporteProductos = total,
        Subtotal = total,
        Total = total,
        CostoTotal = total / 2m,
        UtilidadBruta = total / 2m
    };
}
