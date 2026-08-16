using System.Reflection;
using InventoryApp.API.Controllers;
using InventoryApp.API.Filters;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N15KardexApiContractTests
{
    [Fact]
    public void Controller_ExigeAutenticacion_Y_Ruta_Canonica()
    {
        var tipo = typeof(MovimientosInventarioController);

        Assert.NotNull(tipo.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(tipo.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Equal("inventario/movimientos", tipo.GetCustomAttribute<RouteAttribute>()?.Template);

        foreach (var metodo in MetodosHttp(tipo))
            Assert.Null(metodo.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(nameof(MovimientosInventarioController.GetFiltered), null)]
    [InlineData(nameof(MovimientosInventarioController.GetPaged), "paged")]
    public void Endpoints_De_Lectura_Exigen_Permiso_Relacional_Ver(string nombreMetodo, string? plantilla)
    {
        var metodo = typeof(MovimientosInventarioController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Single(m => m.Name == nombreMetodo);

        var httpGet = Assert.Single(metodo.GetCustomAttributes<HttpGetAttribute>());
        Assert.Equal(plantilla, httpGet.Template);

        var permiso = Assert.Single(metodo.GetCustomAttributes<RequierePermisoAttribute>());
        Assert.Equal(ModuloSistema.MovimientosInventario, LeerCampoPrivado<ModuloSistema>(permiso, "_modulo"));
        Assert.Equal(AccionPermiso.Ver, LeerCampoPrivado<AccionPermiso>(permiso, "_accion"));
    }

    private static IEnumerable<MethodInfo> MetodosHttp(Type tipo) =>
        tipo.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any());

    private static T LeerCampoPrivado<T>(object instancia, string nombre)
    {
        var campo = instancia.GetType().GetField(nombre, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(campo);
        return Assert.IsType<T>(campo!.GetValue(instancia));
    }
}
