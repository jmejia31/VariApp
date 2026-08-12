using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryApp.Tests;

public class MovimientoInventarioModeloOrigenTipadoTests
{
    [Fact]
    public void ModeloEf_Mapea_LasTresFksTipadas_SobreColumnasExistentes()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n06-d2a-{Guid.NewGuid():N}")
            .Options;
        using var context = new AppDbContext(options);

        var entity = context.Model.FindEntityType(typeof(MovimientoInventario));
        Assert.NotNull(entity);

        Assert.NotNull(entity!.FindProperty(nameof(MovimientoInventario.CompraId)));
        Assert.NotNull(entity.FindProperty(nameof(MovimientoInventario.VentaId)));
        Assert.NotNull(entity.FindProperty(nameof(MovimientoInventario.ConsumoInsumoId)));

        Assert.Contains(entity.GetForeignKeys(), fk =>
            fk.Properties.Single().Name == nameof(MovimientoInventario.CompraId) &&
            fk.PrincipalEntityType.ClrType == typeof(Compra));
        Assert.Contains(entity.GetForeignKeys(), fk =>
            fk.Properties.Single().Name == nameof(MovimientoInventario.VentaId) &&
            fk.PrincipalEntityType.ClrType == typeof(Venta));
        Assert.Contains(entity.GetForeignKeys(), fk =>
            fk.Properties.Single().Name == nameof(MovimientoInventario.ConsumoInsumoId) &&
            fk.PrincipalEntityType.ClrType == typeof(ConsumoInsumo));
    }
}
