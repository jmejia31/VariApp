using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Xunit;

namespace InventoryApp.Tests;

public class AlmacenDomainTests
{
    [Fact]
    public void TipoAlmacen_MantieneClasificacionesOperacionalesEstables()
    {
        Assert.Equal(1, (int)TipoAlmacen.Tienda);
        Assert.Equal(2, (int)TipoAlmacen.Bodega);
        Assert.Equal(3, (int)TipoAlmacen.Transito);
        Assert.Equal(4, (int)TipoAlmacen.Devolucion);
        Assert.Equal(5, (int)TipoAlmacen.Cuarentena);
        Assert.Equal(5, Enum.GetValues<TipoAlmacen>().Length);
    }

    [Fact]
    public void Almacen_Nuevo_IniciaActivoYSinEliminacion()
    {
        var almacen = new Almacen();

        Assert.True(almacen.Activo);
        Assert.False(almacen.Eliminado);
        Assert.Null(almacen.FechaEliminacion);
        Assert.Null(almacen.EliminadoPorUsuarioId);
    }
}
