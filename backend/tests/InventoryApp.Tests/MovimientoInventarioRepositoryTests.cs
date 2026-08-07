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
            new[] { new InventarioDemanda(10, 101, 1) });

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
            new[] { new InventarioDemanda(20, 201, 1) });

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
            new[] { new InventarioDemanda(30, null, 1) });
        Assert.False(conOtraVariante);

        context.MovimientosInventario.Add(CrearMovimiento(30, null, "Ajuste", 3));
        await context.SaveChangesAsync();

        var conMismaClave = await repo.ExisteMovimientoPosteriorAsync(
            original.Id,
            new[] { new InventarioDemanda(30, null, 1) });
        Assert.True(conMismaClave);
    }

    [Fact]
    public async Task ExisteMovimientoPosteriorAsync_DemandasDuplicadas_SeConsolidanSinCambiarResultado()
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
            new[]
            {
                new InventarioDemanda(40, 401, 1),
                new InventarioDemanda(40, 401, 2)
            });

        Assert.True(existe);
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
