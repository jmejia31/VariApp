using System.Reflection;
using InventoryApp.API.Controllers;
using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace InventoryApp.Tests.API.Controllers;

public sealed class ReportesInventarioAuthorizationContractTests
{
    [Theory]
    [InlineData(typeof(ReportesInventarioValorizacionController), "inventario/reportes/valorizacion")]
    [InlineData(typeof(ReportesInventarioKardexController), "inventario/reportes/kardex")]
    [InlineData(typeof(ReportesInventarioStockHealthController), "inventario/reportes/stock-health")]
    [InlineData(typeof(ReportesInventarioReconciliacionController), "inventario/reportes/reconciliacion")]
    public void ReportController_RequiereAutenticacionYRutaCanonica(Type controllerType, string expectedRoute)
    {
        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>());
        var route = controllerType.GetCustomAttribute<RouteAttribute>();
        Assert.NotNull(route);
        Assert.Equal(expectedRoute, route.Template);
    }

    [Fact]
    public void Valorizacion_RequiereInventarioVer()
        => AssertPermission(typeof(ReportesInventarioValorizacionController), nameof(ReportesInventarioValorizacionController.GetResumen), ModuloSistema.Inventario, AccionPermiso.Ver);

    [Fact]
    public void Kardex_RequiereMovimientosConsultarHistorial()
        => AssertPermission(typeof(ReportesInventarioKardexController), nameof(ReportesInventarioKardexController.Get), ModuloSistema.MovimientosInventario, AccionPermiso.ConsultarHistorial);

    [Fact]
    public void StockHealth_RequiereInventarioVer()
        => AssertPermission(typeof(ReportesInventarioStockHealthController), nameof(ReportesInventarioStockHealthController.Get), ModuloSistema.Inventario, AccionPermiso.Ver);

    [Fact]
    public void Reconciliacion_RequiereInventarioVer()
        => AssertPermission(typeof(ReportesInventarioReconciliacionController), nameof(ReportesInventarioReconciliacionController.Get), ModuloSistema.Inventario, AccionPermiso.Ver);

    [Fact]
    public void QueryRules_RechazaVentanaMayorA366Dias()
    {
        var filtro = new ReporteInventarioKardexFiltroDto
        {
            Desde = new DateTime(2026, 1, 1),
            Hasta = new DateTime(2027, 1, 3),
            SortBy = "Fecha",
            SortDirection = "desc"
        };

        var error = ReporteInventarioQueryRules.Validate(filtro, "Fecha");
        Assert.NotNull(error);
        Assert.Contains("366", error);
    }

    [Fact]
    public void QueryRules_RechazaDireccionYSortNoPermitidos()
    {
        var direccion = new ReporteInventarioKardexFiltroDto { SortBy = "Fecha", SortDirection = "sideways" };
        Assert.NotNull(ReporteInventarioQueryRules.Validate(direccion, "Fecha"));

        var sort = new ReporteInventarioKardexFiltroDto { SortBy = "CostoSecreto", SortDirection = "desc" };
        Assert.NotNull(ReporteInventarioQueryRules.Validate(sort, "Fecha"));
    }

    [Fact]
    public void QueryRules_StockHealthMantieneDiasAcotados()
    {
        var filtro = new ReporteInventarioStockHealthFiltroDto
        {
            Dias = ReporteInventarioQueryRules.MaxHistoricalDays + 1,
            SortBy = "Fecha",
            SortDirection = "desc"
        };

        var error = ReporteInventarioQueryRules.ValidateStockHealth(filtro);
        Assert.NotNull(error);
        Assert.Contains("Dias", error);
    }

    [Fact]
    public async Task ScopeGuard_SinFiltroFisico_ConservaScopeServerSideExistente()
    {
        var filtro = new ReporteInventarioStockHealthFiltroDto();
        var permitido = await ReporteInventarioScopeGuard.CanUseExplicitPhysicalScopeAsync(
            filtro,
            new FakeScopeService(null));

        Assert.True(permitido);
    }

    [Fact]
    public async Task ScopeGuard_FiltroFisicoExplicito_NoAdmin_FallaCerrado()
    {
        var filtro = new ReporteInventarioStockHealthFiltroDto { AlmacenId = 7 };
        var permitido = await ReporteInventarioScopeGuard.CanUseExplicitPhysicalScopeAsync(
            filtro,
            new FakeScopeService(new UsuarioScopeActual(41, 3, "Operador", false)));

        Assert.False(permitido);
    }

    [Fact]
    public async Task ScopeGuard_FiltroFisicoExplicito_Admin_Permitido()
    {
        var filtro = new ReporteInventarioReconciliacionFiltroDto { SucursalId = 2 };
        var permitido = await ReporteInventarioScopeGuard.CanUseExplicitPhysicalScopeAsync(
            filtro,
            new FakeScopeService(new UsuarioScopeActual(1, 1, "Administrador", true)));

        Assert.True(permitido);
    }

    [Fact]
    public async Task ScopeGuard_FiltroFisicoExplicito_ScopeNoResuelto_FallaCerrado()
    {
        var filtro = new ReporteInventarioKardexFiltroDto { UbicacionAlmacenId = 9 };
        var permitido = await ReporteInventarioScopeGuard.CanUseExplicitPhysicalScopeAsync(
            filtro,
            new FakeScopeService(null));

        Assert.False(permitido);
    }

    private static void AssertPermission(Type controllerType, string methodName, ModuloSistema expectedModule, AccionPermiso expectedAction)
    {
        var method = controllerType.GetMethod(methodName);
        Assert.NotNull(method);
        Assert.NotNull(method.GetCustomAttribute<HttpGetAttribute>());

        var permiso = method.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permiso);

        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);

        Assert.Equal(expectedModule, (ModuloSistema?)moduloField.GetValue(permiso));
        Assert.Equal(expectedAction, (AccionPermiso?)accionField.GetValue(permiso));
    }

    private sealed class FakeScopeService : IUsuarioScopeService
    {
        private readonly UsuarioScopeActual? _scope;

        public FakeScopeService(UsuarioScopeActual? scope) => _scope = scope;

        public Task<UsuarioScopeActual?> ObtenerActualAsync() => Task.FromResult(_scope);
    }
}
