using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class MovimientoInventarioRepositoryTests
{
    [Fact]
    public async Task ExisteMovimientoPosteriorAsync_OtraVarianteDelMismoProducto_NoBloquea()
    {
        await using var context = CrearContexto();
        var scope = CrearScopeAdministrador();
        var repo = new MovimientoInventarioRepository(context, scope.Object);

        var original = CrearMovimiento(10, 101, "Compra", 1);
        context.MovimientosInventario.Add(original);
        await context.SaveChangesAsync();

        context.MovimientosInventario.Add(CrearMovimiento(10, 102, "Venta", 2));
        await context.SaveChangesAsync();

        var existe = await repo.ExisteMovimientoPosteriorAsync(
            original.Id,
            new[] { 10 });

        Assert.False(existe);
    }

    [Fact]
    public async Task ExisteMovimientoPosteriorAsync_MismaVariante_Bloquea()
    {
        await using var context = CrearContexto();
        var scope = CrearScopeAdministrador();
        var repo = new MovimientoInventarioRepository(context, scope.Object);

        var original = CrearMovimiento(20, 201, "Compra", 1);
        context.MovimientosInventario.Add(original);
        await context.SaveChangesAsync();

        context.MovimientosInventario.Add(CrearMovimiento(20, 201, "Venta", 2));
        await context.SaveChangesAsync();

        var existe = await repo.ExisteMovimientoPosteriorAsync(
            original.Id,
            new[] { 20 });

        Assert.True(existe);
    }

    [Fact]
    public async Task ExisteMovimientoPosteriorAsync_ProductoHistoricoSinVariante_ExigeClaveNulaExacta()
    {
        await using var context = CrearContexto();
        var scope = CrearScopeAdministrador();
        var repo = new MovimientoInventarioRepository(context, scope.Object);

        var original = CrearMovimiento(30, null, "Compra", 1);
        context.MovimientosInventario.Add(original);
        await context.SaveChangesAsync();

        context.MovimientosInventario.Add(CrearMovimiento(30, 301, "Ajuste", 2));
        await context.SaveChangesAsync();

        var conOtraVariante = await repo.ExisteMovimientoPosteriorAsync(
            original.Id,
            new[] { 30 });
        Assert.False(conOtraVariante);

        context.MovimientosInventario.Add(CrearMovimiento(30, null, "Ajuste", 3));
        await context.SaveChangesAsync();

        var conMismaClave = await repo.ExisteMovimientoPosteriorAsync(
            original.Id,
            new[] { 30 });
        Assert.True(conMismaClave);
    }

    [Fact]
    public async Task ExisteMovimientoPosteriorAsync_ProductosDuplicados_SeConsolidanSinCambiarResultado()
    {
        await using var context = CrearContexto();
        var scope = CrearScopeAdministrador();
        var repo = new MovimientoInventarioRepository(context, scope.Object);

        var original = CrearMovimiento(40, 401, "Compra", 1);
        context.MovimientosInventario.Add(original);
        await context.SaveChangesAsync();

        context.MovimientosInventario.Add(CrearMovimiento(40, 401, "Venta", 2));
        await context.SaveChangesAsync();

        var existe = await repo.ExisteMovimientoPosteriorAsync(
            original.Id,
            new[] { 40, 40 });

        Assert.True(existe);
    }

    [Fact]
    public async Task ExisteMovimientoPosteriorAsync_CompraConDosVariantes_DetectaSoloClavesOriginales()
    {
        await using var context = CrearContexto();
        var scope = CrearScopeAdministrador();
        var repo = new MovimientoInventarioRepository(context, scope.Object);

        var originalA = CrearMovimiento(50, 501, "Compra", 7);
        var originalB = CrearMovimiento(50, 502, "Compra", 7);
        context.MovimientosInventario.AddRange(originalA, originalB);
        await context.SaveChangesAsync();

        context.MovimientosInventario.Add(CrearMovimiento(50, 503, "Venta", 8));
        await context.SaveChangesAsync();

        Assert.False(await repo.ExisteMovimientoPosteriorAsync(originalB.Id, new[] { 50 }));

        context.MovimientosInventario.Add(CrearMovimiento(50, 502, "Venta", 9));
        await context.SaveChangesAsync();

        Assert.True(await repo.ExisteMovimientoPosteriorAsync(originalB.Id, new[] { 50 }));
    }

    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"movimientos-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static Mock<IUsuarioScopeService> CrearScopeAdministrador()
    {
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Admin", true));
        return scope;
    }

    private static MovimientoInventario CrearMovimiento(
        int productoId,
        int? varianteId,
        string referenciaTipo,
        int referenciaId) => new()
    {
        ProductoId = productoId,
        ProductoVarianteId = varianteId,
        Tipo = referenciaTipo == "Compra" ? TipoMovimientoInventario.Entrada : TipoMovimientoInventario.Salida,
        Cantidad = 1,
        StockAnterior = 1,
        StockNuevo = referenciaTipo == "Compra" ? 2 : 0,
        ReferenciaTipo = referenciaTipo,
        ReferenciaId = referenciaId
    };
}
