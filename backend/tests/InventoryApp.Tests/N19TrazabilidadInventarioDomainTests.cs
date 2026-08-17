using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Xunit;

namespace InventoryApp.Tests;

public class N19TrazabilidadInventarioDomainTests
{
    [Fact]
    public void Variante_sin_configuracion_permanece_sin_trazabilidad()
    {
        var variante = new ProductoVariante();
        Assert.False(variante.RequiereTrazabilidad);
    }

    [Fact]
    public void Vencimiento_requiere_lote_y_alerta_no_negativa()
    {
        var variante = new ProductoVariante();

        Assert.Throws<InvalidOperationException>(() => variante.ConfigurarTrazabilidad(false, false, true, 15));
        Assert.Throws<ArgumentOutOfRangeException>(() => variante.ConfigurarTrazabilidad(true, false, true, -1));

        variante.ConfigurarTrazabilidad(true, false, true, 15);
        Assert.True(variante.ControlaLote);
        Assert.True(variante.ControlaFechaVencimiento);
        Assert.Equal(15, variante.DiasAlertaVencimiento);
    }

    [Fact]
    public void Lote_normaliza_codigo_y_rechaza_vencimiento_anterior_a_fabricacion()
    {
        var lote = new LoteInventario { ProductoVarianteId = 11 };

        Assert.Throws<InvalidOperationException>(() => lote.ConfigurarIdentidad(
            "L-1",
            new DateTime(2026, 8, 10),
            new DateTime(2026, 8, 9),
            true));

        lote.ConfigurarIdentidad(" lote-001 ", new DateTime(2026, 8, 10), new DateTime(2027, 8, 10), true);
        Assert.Equal("LOTE-001", lote.Codigo);
        Assert.False(lote.EstaVencido(new DateTime(2026, 8, 17)));
        Assert.True(lote.VenceDentroDe(new DateTime(2027, 8, 1), 10));
    }

    [Fact]
    public void Lote_exige_vencimiento_solo_cuando_la_politica_lo_requiere()
    {
        var lote = new LoteInventario { ProductoVarianteId = 11 };

        Assert.Throws<InvalidOperationException>(() => lote.ConfigurarIdentidad("L-1", null, null, true));

        lote.ConfigurarIdentidad("L-1", null, null, false);
        Assert.Null(lote.FechaVencimiento);
    }

    [Fact]
    public void Serie_normaliza_identidad_y_lifecycle_es_fail_closed()
    {
        var serie = new SerieInventario { ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad(" sn-001 ");

        Assert.Equal("SN-001", serie.NumeroSerie);
        Assert.Equal(EstadoSerieInventario.Disponible, serie.Estado);

        serie.Reservar();
        Assert.Equal(EstadoSerieInventario.Reservada, serie.Estado);
        Assert.Throws<InvalidOperationException>(() => serie.Reservar());

        serie.MarcarEnTransito();
        Assert.Equal(EstadoSerieInventario.EnTransito, serie.Estado);
        serie.RecibirDeTransito();
        Assert.Equal(EstadoSerieInventario.Disponible, serie.Estado);
        serie.Vender();
        Assert.Equal(EstadoSerieInventario.Vendida, serie.Estado);
        Assert.Throws<InvalidOperationException>(() => serie.DarDeBaja());
    }
}
