using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryApp.Tests;

public sealed class ProductoVarianteTecnicaModelTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void Modelo_Configura_EsTecnica_Con_Valor_Predeterminado_Falso()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(ProductoVariante));

        var property = entityType!.FindProperty(nameof(ProductoVariante.EsTecnica));

        Assert.NotNull(property);
        Assert.Equal(false, property!.GetDefaultValue());
    }

    [Fact]
    public void Modelo_Configura_Clave_Generada_Para_Unicidad_Tecnica()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(ProductoVariante));

        var property = entityType!.FindProperty("ProductoTecnicoUnico");
        var index = entityType.GetIndexes()
            .Single(x => x.GetDatabaseName() == "IX_ProductoVariantes_ProductoTecnicoUnico");

        Assert.NotNull(property);
        Assert.True(index.IsUnique);
        Assert.Equal(
            "CASE WHEN `EsTecnica` = 1 AND `Eliminado` = 0 THEN `ProductoId` ELSE NULL END",
            property!.GetComputedColumnSql());
        Assert.True(property.GetIsStored());
    }

    [Fact]
    public void Entidad_Nueva_No_Es_Tecnica_Por_Defecto()
    {
        var variante = new ProductoVariante();

        Assert.False(variante.EsTecnica);
    }
}
