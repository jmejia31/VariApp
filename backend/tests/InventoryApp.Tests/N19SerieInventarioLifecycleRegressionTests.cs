using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Xunit;

namespace InventoryApp.Tests;

public class N19SerieInventarioLifecycleRegressionTests
{
    [Fact]
    public void Serie_reservada_puede_venderse_y_no_liberarse_despues()
    {
        var serie = new SerieInventario { ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad("SN-VENTA-RESERVADA");
        serie.Reservar();

        serie.Vender();

        Assert.Equal(EstadoSerieInventario.Vendida, serie.Estado);
        Assert.Throws<InvalidOperationException>(() => serie.LiberarReserva());
        Assert.Equal(EstadoSerieInventario.Vendida, serie.Estado);
    }

    [Fact]
    public void Serie_en_transito_rechaza_venta_y_conserva_estado()
    {
        var serie = new SerieInventario { ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad("SN-TRANSITO-VENTA");
        serie.MarcarEnTransito();

        Assert.Throws<InvalidOperationException>(() => serie.Vender());

        Assert.Equal(EstadoSerieInventario.EnTransito, serie.Estado);
    }

    [Fact]
    public void Dar_de_baja_repetido_es_fail_closed_y_no_muta_estado_terminal()
    {
        var serie = new SerieInventario { ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad("SN-BAJA-IDEMPOTENTE");
        serie.DarDeBaja();

        Assert.Equal(EstadoSerieInventario.Baja, serie.Estado);
        Assert.Throws<InvalidOperationException>(() => serie.DarDeBaja());
        Assert.Equal(EstadoSerieInventario.Baja, serie.Estado);
    }
}
