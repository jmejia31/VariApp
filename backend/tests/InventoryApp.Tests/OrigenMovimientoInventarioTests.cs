using InventoryApp.Domain.Common;
using InventoryApp.Domain.Enums;
using Xunit;

namespace InventoryApp.Tests;

public class OrigenMovimientoInventarioTests
{
    [Fact]
    public void DesdeCompra_ExponeSoloCompraComoOrigen()
    {
        var origen = OrigenMovimientoInventario.DesdeCompra(10);

        Assert.Equal(TipoOrigenMovimientoInventario.Compra, origen.Tipo);
        Assert.Equal(10, origen.DocumentoId);
        Assert.Equal(10, origen.CompraId);
        Assert.Null(origen.VentaId);
        Assert.Null(origen.ConsumoInsumoId);
        Assert.Null(origen.AjusteInventarioId);
    }

    [Fact]
    public void DesdeVenta_ExponeSoloVentaComoOrigen()
    {
        var origen = OrigenMovimientoInventario.DesdeVenta(20);

        Assert.Equal(TipoOrigenMovimientoInventario.Venta, origen.Tipo);
        Assert.Equal(20, origen.VentaId);
        Assert.Null(origen.CompraId);
        Assert.Null(origen.ConsumoInsumoId);
        Assert.Null(origen.AjusteInventarioId);
    }

    [Fact]
    public void DesdeConsumoInsumo_ExponeSoloConsumoComoOrigen()
    {
        var origen = OrigenMovimientoInventario.DesdeConsumoInsumo(30);

        Assert.Equal(TipoOrigenMovimientoInventario.ConsumoInsumo, origen.Tipo);
        Assert.Equal(30, origen.ConsumoInsumoId);
        Assert.Null(origen.CompraId);
        Assert.Null(origen.VentaId);
        Assert.Null(origen.AjusteInventarioId);
    }

    [Fact]
    public void DesdeAjusteInventario_ExponeSoloAjusteComoOrigen()
    {
        var origen = OrigenMovimientoInventario.DesdeAjusteInventario(40);

        Assert.Equal(TipoOrigenMovimientoInventario.AjusteInventario, origen.Tipo);
        Assert.Equal(40, origen.DocumentoId);
        Assert.Equal(40, origen.AjusteInventarioId);
        Assert.Null(origen.CompraId);
        Assert.Null(origen.VentaId);
        Assert.Null(origen.ConsumoInsumoId);
    }

    [Fact]
    public void DesdeIds_SinOrigen_FallaCerrado()
    {
        Assert.Throws<InvalidOperationException>(() =>
            OrigenMovimientoInventario.DesdeIds(null, null, null, null));
    }

    [Fact]
    public void DesdeIds_ConMasDeUnOrigen_FallaCerrado()
    {
        Assert.Throws<InvalidOperationException>(() =>
            OrigenMovimientoInventario.DesdeIds(1, 2, null, null));

        Assert.Throws<InvalidOperationException>(() =>
            OrigenMovimientoInventario.DesdeIds(null, null, 3, 4));
    }

    [Fact]
    public void DesdeIds_ConAjusteInventario_ConstruyeOrigenTipado()
    {
        var origen = OrigenMovimientoInventario.DesdeIds(null, null, null, 40);

        Assert.Equal(TipoOrigenMovimientoInventario.AjusteInventario, origen.Tipo);
        Assert.Equal(40, origen.AjusteInventarioId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void OrigenConIdNoPositivo_EsRechazado(int id)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OrigenMovimientoInventario.DesdeCompra(id));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OrigenMovimientoInventario.DesdeAjusteInventario(id));
    }
}
