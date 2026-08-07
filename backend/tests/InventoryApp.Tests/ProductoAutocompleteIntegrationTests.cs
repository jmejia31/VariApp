using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryApp.Tests;

[Trait("Category", "Integration")]
public sealed class ProductoAutocompleteIntegrationTests
{
    private static DbContextOptions<AppDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                $"Server=localhost;Port=3306;Database={dbName};User=root;Password=root;",
                new MySqlServerVersion(new Version(8, 4, 3)))
            .Options;

    [Fact]
    public async Task Autocomplete_MySql84_FiltraStockVenta_PermiteStockCeroCompra_YBuscaPorCamposOperativos()
    {
        var dbName = $"test_autocomplete_2c5_{Guid.NewGuid():N}";
        var options = CreateOptions(dbName);

        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.MigrateAsync();

            var conStock = CrearProducto("Buds Pro Remoto", "VariStorehn", "2C5", 4);
            var sinStock = CrearProducto("Cable Remoto", "VariStorehn", "USB-C", 0);
            setup.Productos.AddRange(conStock, sinStock);
            await setup.SaveChangesAsync();

            setup.ProductoVariantes.AddRange(
                CrearVariante(conStock, "SKU-BUDS-2C5", "000000002501", 4, 120m, 220m),
                CrearVariante(sinStock, "SKU-CABLE-2C5", "000000002502", 0, 35m, 80m));
            await setup.SaveChangesAsync();
        }

        await using var context = new AppDbContext(options);
        try
        {
            var service = new ProductoEscanerService(new ProductoVarianteRepository(context));

            var ventaPorNombre = await service.BuscarParaVentaAsync("buds pro");
            Assert.Single(ventaPorNombre);
            Assert.Equal("SKU-BUDS-2C5", ventaPorNombre[0].Sku);
            Assert.Equal(4, ventaPorNombre[0].CantidadDisponible);

            var ventaSinStock = await service.BuscarParaVentaAsync("cable remoto");
            Assert.Empty(ventaSinStock);

            var compraSinStock = await service.BuscarParaCompraAsync("sku-cable");
            Assert.Single(compraSinStock);
            Assert.Equal(0, compraSinStock[0].CantidadDisponible);
            Assert.Equal(35m, compraSinStock[0].Costo);

            var compraPorCodigoConCeros = await service.BuscarParaCompraAsync("000000002502");
            Assert.Single(compraPorCodigoConCeros);
            Assert.Equal("000000002502", compraPorCodigoConCeros[0].CodigoBarras);

            var ventaPorMarca = await service.BuscarParaVentaAsync("varistorehn");
            Assert.Single(ventaPorMarca);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    private static Producto CrearProducto(string nombre, string marca, string modelo, int cantidad) =>
        new()
        {
            Nombre = nombre,
            Marca = marca,
            Modelo = modelo,
            Cantidad = cantidad,
            Costo = 40m,
            Precio = 80m,
            UmbralStockBajo = 2,
            Activo = true,
            Eliminado = false
        };

    private static ProductoVariante CrearVariante(
        Producto producto,
        string sku,
        string codigoBarras,
        int cantidad,
        decimal costo,
        decimal precio) =>
        new()
        {
            ProductoId = producto.Id,
            Producto = producto,
            Sku = sku,
            CodigoBarras = codigoBarras,
            Cantidad = cantidad,
            Costo = costo,
            Precio = precio,
            UmbralStockBajo = 2,
            Activo = true,
            Eliminado = false
        };
}
