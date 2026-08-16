using InventoryApp.Domain.Common;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Tests;

public class OrigenMovimientoInventarioTests
{
    [Theory]
    [InlineData(TipoOrigenMovimientoInventario.Compra, 11)]
    [InlineData(TipoOrigenMovimientoInventario.Venta, 22)]
    [InlineData(TipoOrigenMovimientoInventario.ConsumoInsumo, 33)]
    [InlineData(TipoOrigenMovimientoInventario.AjusteInventario, 44)]
    [InlineData(TipoOrigenMovimientoInventario.TransferenciaInventario, 55)]
    public void Factory_CreaOrigenTipadoValido(TipoOrigenMovimientoInventario tipo, int id)
    {
        var origen = tipo switch
        {
            TipoOrigenMovimientoInventario.Compra => OrigenMovimientoInventario.DesdeCompra(id),
            TipoOrigenMovimientoInventario.Venta => OrigenMovimientoInventario.DesdeVenta(id),
            TipoOrigenMovimientoInventario.ConsumoInsumo => OrigenMovimientoInventario.DesdeConsumoInsumo(id),
            TipoOrigenMovimientoInventario.AjusteInventario => OrigenMovimientoInventario.DesdeAjusteInventario(id),
            TipoOrigenMovimientoInventario.TransferenciaInventario => OrigenMovimientoInventario.DesdeTransferenciaInventario(id),
            _ => throw new InvalidOperationException()
        };

        Assert.Equal(tipo, origen.Tipo);
        Assert.Equal(id, origen.Id);
    }

    [Fact]
    public void DesdeIds_FallaSiNoHayOrigen()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => OrigenMovimientoInventario.DesdeIds(null, null, null, null, null));
        Assert.Contains("exactamente un origen", exception.Message);
    }

    [Fact]
    public void DesdeIds_FallaSiHayMasDeUnOrigen()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => OrigenMovimientoInventario.DesdeIds(
                compraId: 1,
                ventaId: null,
                consumoInsumoId: null,
                ajusteInventarioId: null,
                transferenciaInventarioId: 2));
        Assert.Contains("exactamente un origen", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Factory_FallaSiIdNoEsPositivo(int id)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OrigenMovimientoInventario.DesdeCompra(id));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrigenMovimientoInventario.DesdeTransferenciaInventario(id));
    }
}
