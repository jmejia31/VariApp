using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N19TrazabilidadInventarioApplicationTests
{
    [Fact]
    public async Task Configurar_rechaza_habilitar_dimension_nueva_si_hay_stock_existente()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.TieneStockFisicoAsync(11)).ReturnsAsync(true);
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var service = CrearService(repo, variantes);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.ConfigurarAsync(11, new ConfigurarTrazabilidadVarianteRequest
        {
            ControlaLote = true
        }));

        Assert.Contains("adopción/apertura", ex.Message);
        variantes.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Crear_lote_es_idempotente_por_variante_y_codigo_si_payload_es_equivalente()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        variante.ConfigurarTrazabilidad(true, false, true, 30);
        var existente = new LoteInventario { Id = 7, ProductoVarianteId = 11 };
        existente.ConfigurarIdentidad("lote-001", new DateTime(2026, 8, 1), new DateTime(2027, 8, 1), true);

        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetLoteByCodigoAsync(11, "LOTE-001", false)).ReturnsAsync(existente);
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var service = CrearService(repo, variantes);

        var resultado = await service.CrearLoteAsync(new CrearLoteInventarioRequest
        {
            ProductoVarianteId = 11,
            Codigo = " lote-001 ",
            FechaFabricacion = new DateTime(2026, 8, 1),
            FechaVencimiento = new DateTime(2027, 8, 1)
        });

        Assert.Equal(7, resultado.Id);
        repo.Verify(x => x.TryAddLoteAsync(It.IsAny<LoteInventario>()), Times.Never);
    }

    [Fact]
    public async Task Crear_serie_con_lote_opcional_respeta_contrato_B()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        variante.ConfigurarTrazabilidad(true, true, false);
        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetSerieByNumeroAsync("SN-001", false)).ReturnsAsync((SerieInventario?)null);
        repo.Setup(x => x.TryAddSerieAsync(It.IsAny<SerieInventario>())).ReturnsAsync(true);
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var service = CrearService(repo, variantes);

        var resultado = await service.CrearSerieAsync(new CrearSerieInventarioRequest
        {
            ProductoVarianteId = 11,
            NumeroSerie = " sn-001 ",
            LoteInventarioId = null
        });

        Assert.Equal("SN-001", resultado.NumeroSerie);
        Assert.Null(resultado.LoteInventarioId);
        repo.Verify(x => x.TryAddSerieAsync(It.Is<SerieInventario>(s => s.ProductoVarianteId == 11 && s.LoteInventarioId == null)), Times.Once);
    }

    [Fact]
    public async Task Crear_serie_rechaza_lote_de_otra_variante()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        variante.ConfigurarTrazabilidad(true, true, false);
        var loteAjeno = new LoteInventario { Id = 8, ProductoVarianteId = 12 };
        loteAjeno.ConfigurarIdentidad("L-12", null, null, false);

        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetLoteByIdAsync(8, false)).ReturnsAsync(loteAjeno);
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var service = CrearService(repo, variantes);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CrearSerieAsync(new CrearSerieInventarioRequest
        {
            ProductoVarianteId = 11,
            NumeroSerie = "SN-X",
            LoteInventarioId = 8
        }));

        repo.Verify(x => x.TryAddSerieAsync(It.IsAny<SerieInventario>()), Times.Never);
    }

    [Fact]
    public async Task Crear_serie_rechaza_lote_inactivo_sin_persistir_serie()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        variante.ConfigurarTrazabilidad(true, true, false);
        var lote = new LoteInventario { Id = 8, ProductoVarianteId = 11 };
        lote.ConfigurarIdentidad("L-11", null, null, false);
        lote.Desactivar();

        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetLoteByIdAsync(8, false)).ReturnsAsync(lote);
        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var service = CrearService(repo, variantes);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CrearSerieAsync(new CrearSerieInventarioRequest
        {
            ProductoVarianteId = 11,
            NumeroSerie = "SN-INACTIVA",
            LoteInventarioId = 8
        }));

        Assert.Contains("inactivo", ex.Message, StringComparison.OrdinalIgnoreCase);
        repo.Verify(x => x.GetSerieByNumeroAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        repo.Verify(x => x.TryAddSerieAsync(It.IsAny<SerieInventario>()), Times.Never);
    }

    [Fact]
    public async Task Desactivar_lote_rechaza_si_existen_series_activas()
    {
        var lote = new LoteInventario { Id = 8, ProductoVarianteId = 11 };
        lote.ConfigurarIdentidad("L-11", null, null, false);
        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetLoteByIdAsync(8, true)).ReturnsAsync(lote);
        repo.Setup(x => x.TieneSeriesActivasEnLoteAsync(8)).ReturnsAsync(true);
        var service = CrearService(repo, new Mock<IProductoVarianteRepository>());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.DesactivarLoteAsync(8));

        Assert.Contains("series activas", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(lote.Activo);
        repo.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Baja_de_serie_es_idempotente()
    {
        var serie = new SerieInventario { Id = 5, ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad("SN-005");
        serie.DarDeBaja();
        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetSerieByIdAsync(5, true)).ReturnsAsync(serie);
        var service = CrearService(repo, new Mock<IProductoVarianteRepository>());

        var resultado = await service.DarDeBajaSerieAsync(5);

        Assert.Equal(EstadoSerieInventario.Baja, resultado.Estado);
        repo.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    private static TrazabilidadInventarioService CrearService(
        Mock<ITrazabilidadInventarioRepository> repo,
        Mock<IProductoVarianteRepository> variantes)
    {
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UsuarioId).Returns(99);
        current.SetupGet(x => x.NombreUsuario).Returns("n19-test");
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        var unit = new Mock<IUnitOfWork>();
        unit.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> operation) => operation());
        return new TrazabilidadInventarioService(repo.Object, variantes.Object, current.Object, unit.Object);
    }
}
