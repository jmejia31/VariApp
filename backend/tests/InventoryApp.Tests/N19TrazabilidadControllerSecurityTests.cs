using InventoryApp.API.Controllers;
using InventoryApp.API.Filters;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N19TrazabilidadControllerSecurityTests
{
    [Fact]
    public void Controller_exige_autenticacion_y_no_expone_AllowAnonymous()
    {
        var type = typeof(TrazabilidadInventarioController);

        Assert.NotEmpty(type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
        Assert.Empty(type.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
        Assert.All(type.GetMethods().Where(m => m.DeclaringType == type), method =>
            Assert.Empty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)));
    }

    [Theory]
    [InlineData(nameof(TrazabilidadInventarioController.GetConfiguracion), AccionPermiso.Ver)]
    [InlineData(nameof(TrazabilidadInventarioController.Configurar), AccionPermiso.Editar)]
    [InlineData(nameof(TrazabilidadInventarioController.GetLotes), AccionPermiso.Ver)]
    [InlineData(nameof(TrazabilidadInventarioController.GetLote), AccionPermiso.Ver)]
    [InlineData(nameof(TrazabilidadInventarioController.CrearLote), AccionPermiso.Crear)]
    [InlineData(nameof(TrazabilidadInventarioController.ActualizarLote), AccionPermiso.Editar)]
    [InlineData(nameof(TrazabilidadInventarioController.DesactivarLote), AccionPermiso.Anular)]
    [InlineData(nameof(TrazabilidadInventarioController.GetSeries), AccionPermiso.Ver)]
    [InlineData(nameof(TrazabilidadInventarioController.GetSerie), AccionPermiso.Ver)]
    [InlineData(nameof(TrazabilidadInventarioController.CrearSerie), AccionPermiso.Crear)]
    [InlineData(nameof(TrazabilidadInventarioController.DarDeBajaSerie), AccionPermiso.Anular)]
    public void Cada_endpoint_mantiene_permiso_relacional_especifico(string metodo, AccionPermiso accionEsperada)
    {
        var method = typeof(TrazabilidadInventarioController).GetMethod(metodo)
            ?? throw new InvalidOperationException($"No se encontró {metodo}.");
        var atributo = Assert.Single(method.GetCustomAttributes(typeof(RequierePermisoAttribute), inherit: true))
            as RequierePermisoAttribute;
        Assert.NotNull(atributo);

        var modulo = (ModuloSistema?)typeof(RequierePermisoAttribute)
            .GetField("_modulo", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(atributo);
        var accion = (AccionPermiso?)typeof(RequierePermisoAttribute)
            .GetField("_accion", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(atributo);

        Assert.Equal(ModuloSistema.MovimientosInventario, modulo);
        Assert.Equal(accionEsperada, accion);
    }
}
