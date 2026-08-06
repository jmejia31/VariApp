using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Repositories;
using InventoryApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

[Trait("Category", "Integration")]
public class InventoryConcurrencyTests
{
    private static string GetConnectionString(string dbName) =>
        $"Server=localhost;Port=3306;Database={dbName};User=root;Password=root;";

    private DbContextOptions<AppDbContext> CreateOptions(string dbName)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(GetConnectionString(dbName), new MySqlServerVersion(new Version(8, 4, 0)))
            .Options;
    }

    [Fact]
    public async Task Concurrency_10VentasConcurrentesSobre5Unidades_Solo5ExitosasYStockFinalCero()
    {
        var dbName = $"test_inv_concurrency_{Guid.NewGuid():N}";
        var options = CreateOptions(dbName);

        int productoId = 0;
        int varianteId = 0;
        var ventaIds = new List<int>();

        // 1. Setup inicial con Migraciones reales y 5 unidades de stock
        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync();

            var producto = new Producto
            {
                Nombre = "Producto Concurrente CI",
                Marca = "Test",
                Modelo = "Model",
                Cantidad = 5,
                Costo = 10m,
                Precio = 20m,
                UmbralStockBajo = 1,
                Activo = true,
                Eliminado = false
            };
            setupContext.Productos.Add(producto);
            await setupContext.SaveChangesAsync();
            productoId = producto.Id;

            var variante = new ProductoVariante
            {
                ProductoId = producto.Id,
                Sku = $"SKU-{Guid.NewGuid():N}",
                Cantidad = 5,
                Costo = 10m,
                Precio = 20m,
                Activo = true,
                Eliminado = false
            };
            setupContext.ProductoVariantes.Add(variante);
            await setupContext.SaveChangesAsync();
            varianteId = variante.Id;

            // Crear 10 borradores de venta para confirmación concurrente
            for (int i = 1; i <= 10; i++)
            {
                var venta = new Venta
                {
                    NumeroVenta = $"VEN-TEST-{i:D4}",
                    ClienteNombre = "Cliente Test",
                    Estado = EstadoDocumento.Borrador,
                    Total = 20m,
                    Subtotal = 20m,
                    Detalles = new List<VentaDetalle>
                    {
                        new VentaDetalle
                        {
                            ProductoId = productoId,
                            ProductoVarianteId = varianteId,
                            ProductoNombreSnapshot = producto.Nombre,
                            Cantidad = 1,
                            PrecioUnitario = 20m,
                            Subtotal = 20m
                        }
                    }
                };
                setupContext.Ventas.Add(venta);
                await setupContext.SaveChangesAsync();
                ventaIds.Add(venta.Id);
            }
        }

        // 2. Ejecutar 10 confirmaciones concurrentes con scopes independientes
        var tasks = ventaIds.Select(async id =>
        {
            await using var context = new AppDbContext(options);
            var uow = new UnitOfWork(context);
            var scopeMock = new Mock<IUsuarioScopeService>();
            scopeMock.Setup(s => s.ObtenerActualAsync()).ReturnsAsync(new UsuarioScopeActual(1, 1, "Admin", true));

            var ventaRepo = new VentaRepository(context, Mock.Of<ICurrentUserService>(), scopeMock.Object);
            var prodRepo = new ProductoRepository(context);
            var varRepo = new ProductoVarianteRepository(context);
            var facturaRepo = new FacturaRepository(context, scopeMock.Object, null);
            var movInvRepo = new MovimientoInventarioRepository(context, scopeMock.Object);
            var movFinRepo = new MovimientoFinancieroRepository(context, scopeMock.Object);

            var empresaMock = new Mock<IEmpresaConfiguracionService>();
            empresaMock.Setup(e => e.GetActivaEntidadAsync()).ReturnsAsync(new EmpresaConfiguracion { NombreComercial = "CI", RTN = "0000" });

            var calculoMock = new Mock<ICalculoService>();
            var auditMock = new Mock<IAuditoriaService>();

            var service = new VentaService(
                ventaRepo, Mock.Of<IClienteRepository>(), prodRepo, varRepo,
                facturaRepo, movInvRepo, movFinRepo, empresaMock.Object,
                calculoMock.Object, Mock.Of<ICurrentUserService>(), uow, auditMock.Object,
                Mock.Of<ITipoClientePredeterminadoResolver>());

            try
            {
                await service.ConfirmarAsync(id);
                return true;
            }
            catch (BusinessRuleException)
            {
                return false;
            }
        }).ToList();

        var results = await Task.WhenAll(tasks);

        int exitosas = results.Count(r => r);
        int fallidas = results.Count(r => !r);

        Assert.Equal(5, exitosas);
        Assert.Equal(5, fallidas);

        // 3. Verificación final de existencias en MySQL
        await using (var verifyContext = new AppDbContext(options))
        {
            var pFinal = await verifyContext.Productos.FindAsync(productoId);
            var vFinal = await verifyContext.ProductoVariantes.FindAsync(varianteId);

            Assert.Equal(0, pFinal!.Cantidad);
            Assert.Equal(0, vFinal!.Cantidad);

            await verifyContext.Database.EnsureDeletedAsync();
        }
    }
}
