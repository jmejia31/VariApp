using InventoryApp.Domain.Enums;
using InventoryApp.Domain.ValueObjects;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N110CosteoDominioTests
{
    [Fact]
    public void PromedioPonderado_con_asignacion_valida_conserva_cantidad_y_costo()
    {
        var asignacion = AsignacionCostoInventario.Crear(4, 125.50m);

        var resultado = ResultadoCosteoInventario.Crear(
            MetodoCosteoInventario.PromedioPonderado,
            4,
            new[] { asignacion });

        Assert.Equal(4, resultado.Cantidad);
        Assert.Equal(502.00m, resultado.CostoTotal);
        Assert.Equal(125.50m, resultado.CostoUnitarioPromedio);
    }

    [Fact]
    public void Resultado_rechaza_cantidad_asignada_distinta_de_la_valorada()
    {
        var asignacion = AsignacionCostoInventario.Crear(3, 10m);

        Assert.Throws<ArgumentException>(() => ResultadoCosteoInventario.Crear(
            MetodoCosteoInventario.PromedioPonderado,
            4,
            new[] { asignacion }));
    }

    [Fact]
    public void Fifo_requiere_capa_durable_en_cada_asignacion()
    {
        var sinCapa = AsignacionCostoInventario.Crear(2, 30m);

        Assert.Throws<ArgumentException>(() => ResultadoCosteoInventario.Crear(
            MetodoCosteoInventario.FIFO,
            2,
            new[] { sinCapa }));
    }

    [Fact]
    public void Fifo_admite_consumo_multicapa_y_calcula_costo_total()
    {
        var asignaciones = new[]
        {
            AsignacionCostoInventario.Crear(2, 10m, 101),
            AsignacionCostoInventario.Crear(3, 12m, 102)
        };

        var resultado = ResultadoCosteoInventario.Crear(
            MetodoCosteoInventario.FIFO,
            5,
            asignaciones);

        Assert.Equal(56m, resultado.CostoTotal);
        Assert.Equal(11.2m, resultado.CostoUnitarioPromedio);
    }
}
