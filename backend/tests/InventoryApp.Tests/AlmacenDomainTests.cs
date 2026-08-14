using InventoryApp.Application.DTOs;
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
    public void Almacen_Nuevo_IniciaActivoSinEliminacionYEmpresaNoForzada()
    {
        var almacen = new Almacen();

        Assert.True(almacen.Activo);
        Assert.False(almacen.Eliminado);
        Assert.Null(almacen.FechaEliminacion);
        Assert.Null(almacen.EliminadoPorUsuarioId);
        Assert.Null(almacen.EmpresaId);
    }

    [Fact]
    public void ContratosAlmacen_PreservanEmpresaIdNullableParaN6()
    {
        var create = new CreateAlmacenDto { EmpresaId = 7, SucursalId = 11 };
        var update = new UpdateAlmacenDto { EmpresaId = 7, SucursalId = 11 };
        var response = new AlmacenDto { EmpresaId = 7, SucursalId = 11 };

        Assert.Equal(7, create.EmpresaId);
        Assert.Equal(7, update.EmpresaId);
        Assert.Equal(7, response.EmpresaId);
        Assert.Equal(11, create.SucursalId);
        Assert.Equal(11, update.SucursalId);
        Assert.Equal(11, response.SucursalId);
    }
}
