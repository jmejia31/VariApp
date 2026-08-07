using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class MovimientoFinancieroRepositoryTests
{
    [Fact]
    public async Task AddAsync_ReversionCompraPendiente_AnulaOriginalSinCrearReversion()
    {
        await using var context = CrearContexto();
        var repo = CrearRepositorio(context);
        var original = CrearOriginal(EstadoMovimientoFinanciero.Pendiente);
        context.MovimientosFinancieros.Add(original);
        await context.SaveChangesAsync();

        await repo.AddAsync(CrearReversion(original.CompraId!.Value));
        await context.SaveChangesAsync();

        var movimientos = await context.MovimientosFinancieros.OrderBy(m => m.Id).ToListAsync();
        var unico = Assert.Single(movimientos);
        Assert.Equal(original.Id, unico.Id);
        Assert.Equal(EstadoMovimientoFinanciero.Anulado, unico.Estado);
        Assert.NotNull(unico.FechaAnulacion);
        Assert.Equal(9, unico.AnuladoPorUsuarioId);
    }

    [Fact]
    public async Task AddAsync_ReversionCompraPagada_ConservaOriginalYCreaReversionPendienteVinculada()
    {
        await using var context = CrearContexto();
        var repo = CrearRepositorio(context);
        var original = CrearOriginal(EstadoMovimientoFinanciero.Pagado);
        context.MovimientosFinancieros.Add(original);
        await context.SaveChangesAsync();

        await repo.AddAsync(CrearReversion(original.CompraId!.Value));
        await context.SaveChangesAsync();

        var movimientos = await context.MovimientosFinancieros.OrderBy(m => m.Id).ToListAsync();
        Assert.Equal(2, movimientos.Count);
        Assert.Equal(EstadoMovimientoFinanciero.Pagado, movimientos[0].Estado);

        var reversion = movimientos[1];
        Assert.Equal(EstadoMovimientoFinanciero.Pendiente, reversion.Estado);
        Assert.Equal(original.Id, reversion.ReferenciaId);
        Assert.Equal(original.MetodoPago, reversion.MetodoPago);
        Assert.Equal(CategoriaMovimientoFinanciero.Reversion, reversion.Categoria);
        Assert.Equal(TipoMovimientoFinanciero.Ingreso, reversion.Tipo);
    }

    [Fact]
    public async Task AddAsync_ReversionCompraConOriginalAnulado_EsRechazada()
    {
        await using var context = CrearContexto();
        var repo = CrearRepositorio(context);
        var original = CrearOriginal(EstadoMovimientoFinanciero.Anulado);
        context.MovimientosFinancieros.Add(original);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            repo.AddAsync(CrearReversion(original.CompraId!.Value)));

        Assert.Single(await context.MovimientosFinancieros.ToListAsync());
    }

    [Fact]
    public async Task GetByCompraIdAsync_PrefiereMovimientoOriginalSobreReversion()
    {
        await using var context = CrearContexto();
        var repo = CrearRepositorio(context);
        var original = CrearOriginal(EstadoMovimientoFinanciero.Pagado);
        context.MovimientosFinancieros.Add(original);
        await context.SaveChangesAsync();
        await repo.AddAsync(CrearReversion(original.CompraId!.Value));
        await context.SaveChangesAsync();

        var encontrado = await repo.GetByCompraIdAsync(original.CompraId.Value);

        Assert.NotNull(encontrado);
        Assert.Equal(original.Id, encontrado!.Id);
        Assert.Equal("Compra", encontrado.ModuloOrigen);
    }

    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"finanzas-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static MovimientoFinancieroRepository CrearRepositorio(AppDbContext context)
    {
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(9, 1, "Admin", true));
        return new MovimientoFinancieroRepository(context, scope.Object);
    }

    private static MovimientoFinanciero CrearOriginal(EstadoMovimientoFinanciero estado) => new()
    {
        Tipo = TipoMovimientoFinanciero.Egreso,
        Categoria = CategoriaMovimientoFinanciero.Compra,
        Concepto = "Compra original",
        Monto = 100m,
        Estado = estado,
        MetodoPago = MetodoPago.Transferencia,
        EsAutomatico = true,
        ModuloOrigen = "Compra",
        ReferenciaId = 77,
        CompraId = 77,
        CreadoPorUsuarioId = 9,
        CreadoPorNombreUsuario = "Admin"
    };

    private static MovimientoFinanciero CrearReversion(int compraId) => new()
    {
        Tipo = TipoMovimientoFinanciero.Ingreso,
        Categoria = CategoriaMovimientoFinanciero.Reversion,
        Concepto = "Reversión de compra anulada",
        Descripcion = "Anulación de compra",
        Monto = 100m,
        Estado = EstadoMovimientoFinanciero.Pagado,
        EsAutomatico = true,
        ModuloOrigen = "Reversion",
        ReferenciaId = compraId,
        CompraId = compraId,
        CreadoPorUsuarioId = 9,
        CreadoPorNombreUsuario = "Admin"
    };
}
