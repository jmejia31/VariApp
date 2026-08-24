using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N33ReservaAutomaticaPersistenceTests
{
    [Fact]
    public void ReservaInventario_PedidoVenta_TieneRelacionUnoAUnoOpcionalRestrict()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ReservaInventario));
        Assert.NotNull(entity);

        var property = entity!.FindProperty(nameof(ReservaInventario.PedidoVentaId));
        Assert.NotNull(property);
        Assert.True(property!.IsNullable);

        var index = entity.GetIndexes().Single(x => x.Properties.Count == 1 && x.Properties[0].Name == nameof(ReservaInventario.PedidoVentaId));
        Assert.True(index.IsUnique);
        Assert.Equal("UX_ReservasInventario_PedidoVentaId", index.GetDatabaseName());

        var foreignKey = entity.GetForeignKeys().Single(x => x.Properties.Count == 1 && x.Properties[0].Name == nameof(ReservaInventario.PedidoVentaId));
        Assert.Equal(typeof(PedidoVenta), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        Assert.Equal("FK_ReservasInventario_PedidosVenta_PedidoVentaId", foreignKey.GetConstraintName());
    }

    [Fact]
    public void ReservaInventario_ConservaRelacionVentaExistente()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ReservaInventario));
        Assert.NotNull(entity);

        Assert.NotNull(entity!.FindProperty(nameof(ReservaInventario.VentaId)));
        Assert.Contains(entity.GetForeignKeys(), x => x.Properties.Any(p => p.Name == nameof(ReservaInventario.VentaId)) && x.DeleteBehavior == DeleteBehavior.Restrict);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=localhost;Database=variapp_n33_model;User=root;Password=test;", ServerVersion.Parse("8.0.36-mysql"))
            .Options;
        return new AppDbContext(options);
    }
}
