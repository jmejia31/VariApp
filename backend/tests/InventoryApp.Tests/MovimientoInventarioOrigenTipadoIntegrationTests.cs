using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

[Trait("Category", "Integration")]
public class MovimientoInventarioOrigenTipadoIntegrationTests
{
    private static string GetConnectionString(string dbName) =>
        $"Server=localhost;Port=3306;Database={dbName};User=root;Password=root;";

    private static DbContextOptions<AppDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(GetConnectionString(dbName), new MySqlServerVersion(new Version(8, 4, 3)))
            .Options;

    [Fact]
    public async Task ConsultasDeCompra_UsanCompraId_AunqueSnapshotLegacyNoCoincida()
    {
        var dbName = $"test_n06_d1_{Guid.NewGuid():N}";
        var options = CreateOptions(dbName);

        await using var context = new AppDbContext(options);
        try
        {
            await context.Database.MigrateAsync();

            var producto = new Producto
            {
                Nombre = "Producto N06 D1",
                Marca = "VAEP",
                Modelo = "D1",
                Cantidad = 2,
                Costo = 10m,
                Precio = 15m,
                Activo = true
            };
            var compra = new Compra
            {
                NumeroCompra = $"N06-D1-{Guid.NewGuid():N}",
                ProveedorNombre = "Proveedor D1",
                Estado = EstadoDocumento.Borrador,
                EstadoPago = EstadoPago.Pendiente,
                MetodoPago = MetodoPago.Efectivo
            };
            context.Productos.Add(producto);
            context.Compras.Add(compra);
            await context.SaveChangesAsync();

            // Base aislada de prueba: se retira temporalmente el bridge/constraint C3
            // para poder demostrar que el repositorio decide por CompraId y no por
            // el snapshot legacy. Las FKs reales de C2 permanecen activas.
            await context.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS TR_MovimientosInventario_N06_OrigenTipado_BU");
            await context.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS TR_MovimientosInventario_N06_OrigenTipado_BI");
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE MovimientosInventario DROP CHECK CK_MovimientosInventario_OrigenTipado_Exclusivo_N06");

            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO MovimientosInventario
                    (ProductoId, Tipo, Causa, Cantidad, StockAnterior, StockNuevo,
                     ReferenciaTipo, ReferenciaId, CompraId, VentaId, ConsumoInsumoId, Fecha)
                VALUES
                    ({producto.Id}, 'Entrada', 0, 1, 0, 1,
                     'Venta', 999999, {compra.Id}, NULL, NULL, UTC_TIMESTAMP(6))
                """);

            var originalId = await context.Database
                .SqlQueryRaw<int>("SELECT Id AS Value FROM MovimientosInventario ORDER BY Id DESC LIMIT 1")
                .SingleAsync();

            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO MovimientosInventario
                    (ProductoId, Tipo, Causa, Cantidad, StockAnterior, StockNuevo,
                     ReferenciaTipo, ReferenciaId, CompraId, VentaId, ConsumoInsumoId, Fecha)
                VALUES
                    ({producto.Id}, 'Salida', 0, 1, 1, 0,
                     'AjustePrueba', 123456, NULL, NULL, NULL, UTC_TIMESTAMP(6))
                """);

            var scope = new Mock<IUsuarioScopeService>();
            scope.Setup(s => s.ObtenerActualAsync())
                .ReturnsAsync(new UsuarioScopeActual(1, 1, "Admin", true));
            var repo = new MovimientoInventarioRepository(context, scope.Object);

            var encontrado = await repo.GetUltimoMovimientoOriginalCompraIdAsync(compra.Id);
            Assert.Equal(originalId, encontrado);
            Assert.True(await repo.ExisteMovimientoPosteriorAsync(originalId, new[] { producto.Id }));
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
