using System.Reflection;
using InventoryApp.API.Controllers;
using InventoryApp.API.Filters;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace InventoryApp.Tests.API.Controllers;

public sealed class ReportesInventarioReconciliacionSecurityContractTests
{
    [Fact]
    public void Controller_RequiereAutenticacionPermisoScopeAuditoriaYCensuraFinanciera()
    {
        var type = typeof(ReportesInventarioReconciliacionController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());

        var method = type.GetMethod(nameof(ReportesInventarioReconciliacionController.Get));
        var permiso = method?.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permiso);

        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.Equal(ModuloSistema.Inventario, (ModuloSistema?)moduloField!.GetValue(permiso));
        Assert.Equal(AccionPermiso.Ver, (AccionPermiso?)accionField!.GetValue(permiso));

        var parameters = Assert.Single(type.GetConstructors()).GetParameters().Select(p => p.ParameterType).ToArray();
        Assert.Contains(typeof(IUsuarioScopeService), parameters);
        Assert.Contains(typeof(IPermisoService), parameters);
        Assert.Contains(typeof(IAuditoriaService), parameters);
    }
}
