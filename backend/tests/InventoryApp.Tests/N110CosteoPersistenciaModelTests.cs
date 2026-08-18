using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N110CosteoPersistenciaModelTests
{
    [Fact]
    public void Modelo_EF_contiene_entidades_claves_y_relaciones_de_costeo()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n110-model-{Guid.NewGuid():N}")
            .Options;

        using var db = new AppDbContext(options);
        var model = db.Model;

        var politica = Assert.NotNull(model.FindEntityType(typeof(PoliticaCosteoInventario)));
        Assert.Equal("PoliticasCosteoInventario", politica.GetTableName());
        Assert.Contains(politica.GetIndexes(), i =>
            i.IsUnique && i.Properties.Any(p => p.Name == "EmpresaConfiguracionVigenteId"));

        var estandar = Assert.NotNull(model.FindEntityType(typeof(CostoEstandarInventario)));
        Assert.Contains(estandar.GetKeys(), k =>
            !k.IsPrimaryKey() && k.Properties.Select(p => p.Name).SequenceEqual(new[] { "ProductoVarianteId", "Id" }));
        Assert.Contains(estandar.GetIndexes(), i =>
            i.IsUnique && i.Properties.Any(p => p.Name == "ProductoVarianteVigenteId"));

        var capa = Assert.NotNull(model.FindEntityType(typeof(CapaCostoInventario)));
        Assert.Equal("CapasCostoInventario", capa.GetTableName());
        Assert.Contains(capa.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(UbicacionAlmacen) &&
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { "AlmacenId", "UbicacionAlmacenId" }));
        Assert.Contains(capa.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(CapaCostoInventario) &&
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { "ProductoVarianteId", "CapaCostoOrigenId" }));

        var asignacion = Assert.NotNull(model.FindEntityType(typeof(AsignacionCostoMovimientoInventario)));
        Assert.Contains(asignacion.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(CapaCostoInventario) &&
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { "ProductoVarianteId", "CapaCostoInventarioId" }));

        var variacion = Assert.NotNull(model.FindEntityType(typeof(VariacionCostoEstandarInventario)));
        Assert.Contains(variacion.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(CostoEstandarInventario) &&
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { "ProductoVarianteId", "CostoEstandarInventarioId" }));
    }
}
