using System.Reflection;
using InventoryApp.API.Controllers;
using InventoryApp.API.Filters;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N17ConteosInventarioRbacContractTests
{
    [Fact]
    public void Controller_ExigeAutenticacionGlobal()
    {
        Assert.NotNull(typeof(ConteosInventarioController).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Theory]
    [InlineData(nameof(ConteosInventarioController.Buscar), AccionPermiso.Ver)]
    [InlineData(nameof(ConteosInventarioController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(ConteosInventarioController.Create), AccionPermiso.Crear)]
    [InlineData(nameof(ConteosInventarioController.Update), AccionPermiso.Editar)]
    [InlineData(nameof(ConteosInventarioController.Iniciar), AccionPermiso.CambiarEstado)]
    [InlineData(nameof(ConteosInventarioController.Capturar), AccionPermiso.Editar)]
    [InlineData(nameof(ConteosInventarioController.Cerrar), AccionPermiso.Cerrar)]
    [InlineData(nameof(ConteosInventarioController.Aprobar), AccionPermiso.Aprobar)]
    [InlineData(nameof(ConteosInventarioController.Cancelar), AccionPermiso.Anular)]
    public void Endpoint_UsaPermisoEspecificoMovimientosInventario(string metodo, AccionPermiso accion)
    {
        var method = typeof(ConteosInventarioController).GetMethods()
            .Single(x => x.Name == metodo);
        var permiso = method.GetCustomAttribute<RequierePermisoAttribute>();

        Assert.NotNull(permiso);
        var moduloProperty = typeof(RequierePermisoAttribute).GetProperty("Modulo");
        var accionProperty = typeof(RequierePermisoAttribute).GetProperty("Accion");
        Assert.NotNull(moduloProperty);
        Assert.NotNull(accionProperty);
        Assert.Equal(ModuloSistema.MovimientosInventario, moduloProperty!.GetValue(permiso));
        Assert.Equal(accion, accionProperty!.GetValue(permiso));
    }
}
