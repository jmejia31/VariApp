using System.Reflection;
using InventoryApp.API.Controllers;
using InventoryApp.Application.Common;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace InventoryApp.Tests.Controllers;

public class ConciliacionBancariaControllerAuthorizationTests
{
    private const string PermisoAdministrar = "Finanzas.ConciliacionBancaria.Administrar";
    private const string PermisoCerrarPeriodo = "Finanzas.ConciliacionBancaria.CerrarPeriodo";
    private const string PermisoReabrirPeriodo = "Finanzas.ConciliacionBancaria.ReabrirPeriodo";

    [Fact]
    public void MutacionesUsanPermisoAdministrar()
    {
        AssertPermiso(nameof(ConciliacionBancariaController.ImportarEstadoCuenta), PermisoAdministrar);
        AssertPermiso(nameof(ConciliacionBancariaController.ProponerMatch), PermisoAdministrar);
        AssertPermiso(nameof(ConciliacionBancariaController.ConfirmarMatch), PermisoAdministrar);
        AssertPermiso(nameof(ConciliacionBancariaController.DescartarMatch), PermisoAdministrar);
        AssertPermiso(nameof(ConciliacionBancariaController.CrearAjuste), PermisoAdministrar);
    }

    [Fact]
    public void CerrarPeriodoUsaPermisoEspecifico()
    {
        AssertPermiso(nameof(ConciliacionBancariaController.CerrarPeriodo), PermisoCerrarPeriodo);
    }

    [Fact]
    public void ReabrirPeriodoUsaPermisoEspecifico()
    {
        AssertPermiso(nameof(ConciliacionBancariaController.ReabrirPeriodo), PermisoReabrirPeriodo);
    }

    private static void AssertPermiso(string methodName, string expectedPolicy)
    {
        var method = typeof(ConciliacionBancariaController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(PermisosPolicy.Prefix + expectedPolicy, authorize!.Policy);
    }
}
