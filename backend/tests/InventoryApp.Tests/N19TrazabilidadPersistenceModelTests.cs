using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace InventoryApp.Tests;

public class N19TrazabilidadPersistenceModelTests
{
    [Fact]
    public void ProductoVariante_Debe_Persistir_Politica_OptIn_De_Trazabilidad()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n19-variante-{Guid.NewGuid():N}")
            .Options;

        using var db = new AppDbContext(options);
        var variante = db.Model.FindEntityType(typeof(ProductoVariante));

        Assert.NotNull(variante);
        Assert.NotNull(variante!.FindProperty(nameof(ProductoVariante.ControlaLote)));
        Assert.NotNull(variante.FindProperty(nameof(ProductoVariante.ControlaNumeroSerie)));
        Assert.NotNull(variante.FindProperty(nameof(ProductoVariante.ControlaFechaVencimiento)));
        Assert.NotNull(variante.FindProperty(nameof(ProductoVariante.DiasAlertaVencimiento)));
        Assert.Null(variante.FindProperty(nameof(ProductoVariante.RequiereTrazabilidad)));
    }

    [Fact]
    public void Modelo_EF_Debe_Mapear_Lotes_Y_Series_Con_Identidades_Unicas_Y_Fks_Restrictivas()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n19-trazabilidad-{Guid.NewGuid():N}")
            .Options;

        using var db = new AppDbContext(options);
        var lote = db.Model.FindEntityType(typeof(LoteInventario));
        var serie = db.Model.FindEntityType(typeof(SerieInventario));

        Assert.NotNull(lote);
        Assert.NotNull(serie);
        Assert.Equal("LotesInventario", lote!.GetTableName());
        Assert.Equal("SeriesInventario", serie!.GetTableName());

        var loteUnico = lote.GetIndexes().Single(i =>
            i.GetDatabaseName() == "UX_LotesInventario_Variante_Codigo");
        Assert.True(loteUnico.IsUnique);
        Assert.Equal(new[] { "ProductoVarianteId", "Codigo" }, loteUnico.Properties.Select(p => p.Name));

        var serieUnica = serie.GetIndexes().Single(i =>
            i.GetDatabaseName() == "UX_SeriesInventario_NumeroSerie");
        Assert.True(serieUnica.IsUnique);
        Assert.Equal(new[] { "NumeroSerie" }, serieUnica.Properties.Select(p => p.Name));

        Assert.All(lote.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
        Assert.All(serie.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
    }

    [Fact]
    public void DbContext_Debe_Exponer_Lotes_Y_Series()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n19-dbsets-{Guid.NewGuid():N}")
            .Options;

        using var db = new AppDbContext(options);

        Assert.NotNull(db.LotesInventario);
        Assert.NotNull(db.SeriesInventario);
    }
}
