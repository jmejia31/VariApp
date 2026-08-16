using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N16TransferenciaInventarioExistenciaServiceTests
{
    [Fact]
    public async Task AplicarDespacho_DeduceStockFisicoBajoClaveBloqueada()
    {
        var clave = new InventarioExistenciaClave(91, 10, 101);
        var existencia = new ExistenciaVariante
        {
            ProductoVarianteId = 91,
            AlmacenId = 10,
            UbicacionAlmacenId = 101
        };
        existencia.EstablecerStocks(12, 2, 0, 0, null);
        var demanda = new InventarioDemandaExistencia(44, 91, 10, 101, 5);
        var fake = new FakeConcurrency();
        var service = new TransferenciaInventarioExistenciaService(fake);
        var lockSet = new InventarioExistenciaLockSet(
            new Dictionary<InventarioExistenciaClave, ExistenciaVariante> { [clave] = existencia },
            new[] { demanda });

        var transiciones = await service.AplicarDespachoAsync(lockSet);

        var transicion = Assert.Single(transiciones);
        Assert.Equal(12, transicion.StockAnterior);
        Assert.Equal(7, transicion.StockNuevo);
        Assert.Equal(5, transicion.Cantidad);
        Assert.Equal(clave, fake.UltimaClave);
        Assert.Equal(12, fake.UltimoEsperado);
        Assert.Equal(7, fake.UltimoNuevo);
    }

    [Fact]
    public async Task BloquearParaDespacho_SolicitaDeduccionSobreStockDisponible()
    {
        var transferencia = CrearTransferenciaDespachada();
        var fake = new FakeConcurrency();
        var service = new TransferenciaInventarioExistenciaService(fake);

        await service.BloquearParaDespachoAsync(transferencia);

        Assert.True(fake.UltimaEsDeduccion);
        var demanda = Assert.Single(fake.UltimasDemandas);
        Assert.Equal(91, demanda.ProductoVarianteId);
        Assert.Equal(10, demanda.AlmacenId);
        Assert.Equal(101, demanda.UbicacionAlmacenId);
        Assert.Equal(5, demanda.Cantidad);
    }

    private static TransferenciaInventario CrearTransferenciaDespachada()
    {
        var variante = new ProductoVariante { Id = 91, ProductoId = 44, Activo = true };
        var detalle = new TransferenciaInventarioDetalle
        {
            ProductoVarianteId = 91,
            ProductoVariante = variante,
            UbicacionOrigenId = 101,
            CreadoPorUsuarioId = 7
        };
        detalle.EstablecerCantidadSolicitada(6);
        detalle.AprobarCantidad(5);
        detalle.RegistrarDespacho(5);
        return new TransferenciaInventario
        {
            Numero = "TRF-N16-PHY",
            AlmacenOrigenId = 10,
            AlmacenDestinoId = 20,
            CreadoPorUsuarioId = 7,
            Detalles = new List<TransferenciaInventarioDetalle> { detalle }
        };
    }

    private sealed class FakeConcurrency : IExistenciaVarianteConcurrencyService
    {
        public bool UltimaEsDeduccion { get; private set; }
        public IReadOnlyList<InventarioDemandaExistencia> UltimasDemandas { get; private set; } = Array.Empty<InventarioDemandaExistencia>();
        public InventarioExistenciaClave UltimaClave { get; private set; }
        public int UltimoEsperado { get; private set; }
        public int UltimoNuevo { get; private set; }

        public Task<InventarioExistenciaLockSet> BloquearYValidarExistenciasAsync(IEnumerable<InventarioDemandaExistencia> demandas, bool esDeduccion = true)
        {
            UltimaEsDeduccion = esDeduccion;
            UltimasDemandas = demandas.ToList();
            return Task.FromResult(new InventarioExistenciaLockSet(
                new Dictionary<InventarioExistenciaClave, ExistenciaVariante>(), UltimasDemandas));
        }

        public Task<InventarioExistenciaLockSet> BloquearExistenciasParaReversionAsync(IEnumerable<InventarioDemandaExistencia> demandas) =>
            Task.FromResult(new InventarioExistenciaLockSet(
                new Dictionary<InventarioExistenciaClave, ExistenciaVariante>(), demandas.ToList()));

        public Task AjustarStockFisicoPesimistaAsync(InventarioExistenciaClave clave, int cantidadActualEsperada, int cantidadNueva)
        {
            UltimaClave = clave;
            UltimoEsperado = cantidadActualEsperada;
            UltimoNuevo = cantidadNueva;
            return Task.CompletedTask;
        }
    }
}
