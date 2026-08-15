using InventoryApp.Domain.Entities;
using Xunit;

namespace InventoryApp.Tests;

public class N14ExistenciaVarianteQaRegressionTests
{
    [Fact]
    public void EstablecerStocks_FalloDeValidacion_NoDejaEstadoParcialmenteMutado()
    {
        var existencia = new ExistenciaVariante();
        existencia.EstablecerStocks(
            stockFisico: 20,
            stockReservado: 4,
            stockTransito: 3,
            stockMinimo: 5,
            stockMaximo: 40);

        var error = Assert.Throws<ArgumentException>(() =>
            existencia.EstablecerStocks(
                stockFisico: 10,
                stockReservado: 11,
                stockTransito: 99,
                stockMinimo: 1,
                stockMaximo: 2));

        Assert.Equal("stockReservado", error.ParamName);
        Assert.Equal(20, existencia.StockFisico);
        Assert.Equal(4, existencia.StockReservado);
        Assert.Equal(16, existencia.StockDisponible);
        Assert.Equal(3, existencia.StockTransito);
        Assert.Equal(5, existencia.StockMinimo);
        Assert.Equal(40, existencia.StockMaximo);
    }

    [Fact]
    public void EstablecerStocks_ReconfiguracionValida_RecalculaDisponibleSinArrastrarSnapshotAnterior()
    {
        var existencia = new ExistenciaVariante();
        existencia.EstablecerStocks(25, 10, 6, 4, 50);

        existencia.EstablecerStocks(8, 3, 0, 2, null);

        Assert.Equal(8, existencia.StockFisico);
        Assert.Equal(3, existencia.StockReservado);
        Assert.Equal(5, existencia.StockDisponible);
        Assert.Equal(0, existencia.StockTransito);
        Assert.Equal(2, existencia.StockMinimo);
        Assert.Null(existencia.StockMaximo);
        Assert.False(existencia.EstaAgotada);
        Assert.False(existencia.TieneStockBajo);
    }
}
