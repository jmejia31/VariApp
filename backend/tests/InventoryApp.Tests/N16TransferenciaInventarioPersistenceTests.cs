using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryApp.Tests;

public class N16TransferenciaInventarioPersistenceTests
{
    [Fact]
    public void ModeloEf_MapeaCabeceraDetalleConRestriccionesRelacionales()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n16-persistence-{Guid.NewGuid():N}")
            .Options;

        using var context = new AppDbContext(options);
        var transferencia = context.Model.FindEntityType(typeof(TransferenciaInventario));
        var detalle = context.Model.FindEntityType(typeof(TransferenciaInventarioDetalle));

        Assert.NotNull(transferencia);
        Assert.NotNull(detalle);
        Assert.Equal("TransferenciasInventario", transferencia!.GetTableName());
        Assert.Equal("TransferenciaInventarioDetalles", detalle!.GetTableName());

        var numero = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IReadOnlyProperty>(
            transferencia.FindProperty(nameof(TransferenciaInventario.Numero)));
        Assert.Equal(30, numero.GetMaxLength());

        var numeroIndex = Assert.Single(transferencia.GetIndexes()
            .Where(i => i.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { nameof(TransferenciaInventario.Numero) })));
        Assert.True(numeroIndex.IsUnique);
        Assert.Equal("UX_TransferenciasInventario_Numero", numeroIndex.GetDatabaseName());

        var checksCabecera = transferencia.GetCheckConstraints().Select(c => c.Name).ToHashSet();
        Assert.Contains("CK_TransferenciasInventario_AlmacenesDistintos", checksCabecera);
        Assert.Contains("CK_TransferenciasInventario_Estado_Valido", checksCabecera);

        var checksDetalle = detalle.GetCheckConstraints().Select(c => c.Name).ToHashSet();
        Assert.Contains("CK_TransferenciaInventarioDetalles_CantidadesNoNegativas", checksDetalle);
        Assert.Contains("CK_TransferenciaInventarioDetalles_Aprobada", checksDetalle);
        Assert.Contains("CK_TransferenciaInventarioDetalles_Despachada", checksDetalle);
        Assert.Contains("CK_TransferenciaInventarioDetalles_Recepcion", checksDetalle);
        Assert.Null(detalle.FindProperty(nameof(TransferenciaInventarioDetalle.RecepcionCerrada)));

        var cabeceraFk = Assert.Single(detalle.GetForeignKeys()
            .Where(fk => fk.PrincipalEntityType.ClrType == typeof(TransferenciaInventario)));
        Assert.Equal(DeleteBehavior.Cascade, cabeceraFk.DeleteBehavior);

        var almacenFks = transferencia.GetForeignKeys()
            .Where(fk => fk.PrincipalEntityType.ClrType == typeof(Almacen))
            .ToList();
        Assert.Equal(2, almacenFks.Count);
        Assert.All(almacenFks, fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));

        var ubicacionFks = detalle.GetForeignKeys()
            .Where(fk => fk.PrincipalEntityType.ClrType == typeof(UbicacionAlmacen))
            .ToList();
        Assert.Equal(2, ubicacionFks.Count);
        Assert.All(ubicacionFks, fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
    }
}
