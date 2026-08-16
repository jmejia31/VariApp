using System.Globalization;
using InventoryApp.API.Controllers;
using InventoryApp.API.Filters;
using InventoryApp.Domain.Enums;
using Xunit;

namespace InventoryApp.Tests;

public class N17ConteosInventarioControllerRbacTests
{
    [Theory]
    [InlineData(nameof(ConteosInventarioController.Crear), AccionPermiso.Crear)]
    [InlineData(nameof(ConteosInventarioController.Actualizar), AccionPermiso.Editar)]
    [InlineData(nameof(ConteosInventarioController.Iniciar), AccionPermiso.CambiarEstado)]
    [InlineData(nameof(ConteosInventarioController.Cerrar), AccionPermiso.Cerrar)]
    [InlineData(nameof(ConteosInventarioController.Aprobar), AccionPermiso.Aprobar)]
    [InlineData(nameof(ConteosInventarioController.Cancelar), AccionPermiso.Anular)]
    public void Lifecycle_ExigePermisoEspecificoDeMovimientosInventario(
        string metodo,
        AccionPermiso accionEsperada)
    {
        var methodInfo = typeof(ConteosInventarioController).GetMethod(metodo);
        Assert.NotNull(methodInfo);

        var permiso = Assert.Single(methodInfo!.CustomAttributes.Where(a =>
            a.AttributeType == typeof(RequierePermisoAttribute)));

        var modulo = (ModuloSistema)Convert.ToInt32(
            permiso.ConstructorArguments[0].Value,
            CultureInfo.InvariantCulture);
        var accion = (AccionPermiso)Convert.ToInt32(
            permiso.ConstructorArguments[1].Value,
            CultureInfo.InvariantCulture);

        Assert.Equal(ModuloSistema.MovimientosInventario, modulo);
        Assert.Equal(accionEsperada, accion);
    }
}
