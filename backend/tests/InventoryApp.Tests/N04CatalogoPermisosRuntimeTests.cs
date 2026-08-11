using InventoryApp.Application.Common;
using InventoryApp.Domain.Enums;
using Xunit;

namespace InventoryApp.Tests;

public class N04CatalogoPermisosRuntimeTests
{
    [Fact]
    public void Facturacion_IncluyeAdministrar_RequeridoPorCostosEnvio()
    {
        var acciones = CatalogoPermisosBase.Definicion
            .Single(x => x.Modulo == ModuloSistema.Facturacion)
            .Acciones;

        Assert.Contains(AccionPermiso.Administrar, acciones);
    }
}
