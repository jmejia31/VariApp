using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N16TransferenciaInventarioRepositoryNavigationTests
{
    [Fact]
    public async Task GetById_CargaProductoVarianteDeCadaDetalle()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n16-transferencia-nav-{Guid.NewGuid():N}")
            .Options;

        await using var context = new AppDbContext(options);
        var variante = new ProductoVariante
        {
            Id = 91,
            ProductoId = 44,
            Sku = "SKU-N16",
            Activo = true,
            Eliminado = false
        };
        var transferencia = new TransferenciaInventario
        {
            Id = 31,
            Numero = "TRF-NAV-001",
            AlmacenOrigenId = 1,
            AlmacenDestinoId = 2,
            CreadoPorUsuarioId = 7,
            Detalles = new List<TransferenciaInventarioDetalle>
            {
                new()
                {
                    Id = 301,
                    ProductoVarianteId = variante.Id,
                    ProductoVariante = variante,
                    CreadoPorUsuarioId = 7
                }
            }
        };

        context.Set<ProductoVariante>().Add(variante);
        context.Set<TransferenciaInventario>().Add(transferencia);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new TransferenciaInventarioRepository(context);
        var encontrada = await repository.GetByIdAsync(transferencia.Id);

        Assert.NotNull(encontrada);
        var detalle = Assert.Single(encontrada!.Detalles);
        Assert.NotNull(detalle.ProductoVariante);
        Assert.Equal(44, detalle.ProductoVariante.ProductoId);
        Assert.Equal("SKU-N16", detalle.ProductoVariante.Sku);
    }
}
