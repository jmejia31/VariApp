using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Common;
using InventoryApp.Domain.Entities;
using Xunit;

namespace InventoryApp.Tests;

public class MovimientoInventarioCorrelationExtensionsTests
{
    [Fact]
    public async Task AddConOrigenTipadoAsync_ConContextoFisico_MaterializaContextoCompleto()
    {
        var repository = new RepositorySpy();
        var movimiento = new MovimientoInventario();
        var contexto = ContextoFisicoMovimientoInventario.Crear(
            productoVarianteId: 7,
            almacenId: 11,
            ubicacionAlmacenId: 13,
            correlationId: "ajuste-99-abc");

        await repository.AddConOrigenTipadoAsync(
            movimiento,
            OrigenMovimientoInventario.DesdeAjusteInventario(99),
            contexto);

        Assert.Same(movimiento, repository.UltimoMovimiento);
        Assert.Equal(7, movimiento.ProductoVarianteId);
        Assert.Equal(11, movimiento.AlmacenId);
        Assert.Equal(13, movimiento.UbicacionAlmacenId);
        Assert.Equal("ajuste-99-abc", movimiento.CorrelationId);
        Assert.Equal(99, repository.UltimoOrigen?.AjusteInventarioId);
    }

    [Fact]
    public async Task AddConOrigenTipadoCorrelacionadoAsync_NormalizaYPersisteCorrelationId()
    {
        var repository = new RepositorySpy();
        var movimiento = new MovimientoInventario();

        await repository.AddConOrigenTipadoCorrelacionadoAsync(
            movimiento,
            OrigenMovimientoInventario.DesdeCompra(42),
            "  compra-42-abc  ");

        Assert.Same(movimiento, repository.UltimoMovimiento);
        Assert.Equal("compra-42-abc", movimiento.CorrelationId);
        Assert.Equal(42, repository.UltimoOrigen?.CompraId);
        Assert.Null(movimiento.AlmacenId);
        Assert.Null(movimiento.UbicacionAlmacenId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("compra/42")]
    public async Task AddConOrigenTipadoCorrelacionadoAsync_CorrelationIdInvalido_FallaCerrado(string correlationId)
    {
        var repository = new RepositorySpy();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.AddConOrigenTipadoCorrelacionadoAsync(
                new MovimientoInventario(),
                OrigenMovimientoInventario.DesdeCompra(1),
                correlationId));

        Assert.Null(repository.UltimoMovimiento);
    }

    [Fact]
    public async Task AddConOrigenTipadoCorrelacionadoAsync_CorrelationIdExcesivo_FallaCerrado()
    {
        var repository = new RepositorySpy();
        var correlationId = new string('x', ContextoFisicoMovimientoInventario.MaxCorrelationIdLength + 1);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.AddConOrigenTipadoCorrelacionadoAsync(
                new MovimientoInventario(),
                OrigenMovimientoInventario.DesdeCompra(1),
                correlationId));

        Assert.Null(repository.UltimoMovimiento);
    }

    private sealed class RepositorySpy : IMovimientoInventarioRepository
    {
        public MovimientoInventario? UltimoMovimiento { get; private set; }
        public OrigenMovimientoInventario? UltimoOrigen { get; private set; }

        public Task AddAsync(MovimientoInventario movimiento)
        {
            UltimoMovimiento = movimiento;
            return Task.CompletedTask;
        }

        public Task AddConOrigenTipadoAsync(MovimientoInventario movimiento, OrigenMovimientoInventario origen)
        {
            UltimoMovimiento = movimiento;
            UltimoOrigen = origen;
            return Task.CompletedTask;
        }

        public Task<List<MovimientoInventario>> GetByProductoAsync(int productoId) =>
            Task.FromResult(new List<MovimientoInventario>());

        public Task<List<MovimientoInventario>> GetFilteredAsync(
            int? productoId,
            string? tipo,
            DateTime? desde,
            DateTime? hasta) =>
            Task.FromResult(new List<MovimientoInventario>());

        public Task<IReadOnlyDictionary<int, MovimientoInventarioOrigenPersistido>> GetOrigenesTipadosAsync(
            IReadOnlyCollection<int> movimientoIds) =>
            Task.FromResult<IReadOnlyDictionary<int, MovimientoInventarioOrigenPersistido>>(
                new Dictionary<int, MovimientoInventarioOrigenPersistido>());

        public Task<int?> GetUltimoMovimientoOriginalCompraIdAsync(int compraId) =>
            Task.FromResult<int?>(null);

        public Task<bool> ExisteMovimientoPosteriorAsync(
            int ultimoMovimientoOriginalId,
            IReadOnlyCollection<int> productoIds) =>
            Task.FromResult(false);
    }
}
