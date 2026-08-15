using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryApp.Tests;

public class N15KardexPersistenceModelTests
{
    [Fact]
    public void Modelo_Kardex_Persiste_CorrelationId_Requerido_E_Indexado_Por_Fecha()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n15c-model-{Guid.NewGuid():N}")
            .Options;

        using var context = new AppDbContext(options);
        var entity = context.Model.FindEntityType(typeof(MovimientoInventario));

        Assert.NotNull(entity);
        Assert.Equal("MovimientosInventario", entity!.GetTableName());

        var correlationId = entity.FindProperty(nameof(MovimientoInventario.CorrelationId));
        Assert.NotNull(correlationId);
        Assert.False(correlationId!.IsNullable);
        Assert.Equal(100, correlationId.GetMaxLength());
        Assert.Equal(string.Empty, correlationId.GetDefaultValue());

        var correlationIndex = Assert.Single(entity.GetIndexes().Where(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(MovimientoInventario.CorrelationId),
                nameof(MovimientoInventario.Fecha)
            })));

        Assert.Equal(
            "IX_MovimientosInventario_CorrelationId_Fecha",
            correlationIndex.GetDatabaseName());
    }
}
