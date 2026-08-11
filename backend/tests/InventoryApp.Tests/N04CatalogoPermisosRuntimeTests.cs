using InventoryApp.Application.Common;
using InventoryApp.Domain.Enums;
using Xunit;

namespace InventoryApp.Tests;

public class N04CatalogoPermisosRuntimeTests
{
    [Fact]
    public void Facturacion_IncluyeAccionesRequeridasPorControllersRuntime()
    {
        var acciones = CatalogoPermisosBase.Definicion
            .Single(x => x.Modulo == ModuloSistema.Facturacion)
            .Acciones;

        foreach (var accion in new[]
                 {
                     AccionPermiso.Ver,
                     AccionPermiso.Exportar,
                     AccionPermiso.Imprimir,
                     AccionPermiso.Compartir,
                     AccionPermiso.Administrar,
                     AccionPermiso.Aplicar,
                     AccionPermiso.Anular,
                     AccionPermiso.CambiarEstado
                 })
        {
            Assert.Contains(accion, acciones);
        }
    }
}
