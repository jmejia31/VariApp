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
public class ConsumoInsumoIntegrationTests
{
    private static string GetConnectionString(string dbName) =>
        $"Server=localhost;Port=3306;Database={dbName};User=root;Password=root;";

    private static DbContextOptions<AppDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(GetConnectionString(dbName), new MySqlServerVersion(new Version(8, 4, 3)))
            .Options;

    [Fact]
    public async Task Confirmar_Y_Anular_Consumo_Descuenta_Y_Restaura_Sin_Movimiento_Financiero()
    {
        var dbName = $"test_insumos_{Guid.NewGuid():N}";
        var options = CreateOptions(dbName);
        int productoId;
        int varianteId;

        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.MigrateAsync();
            var producto = new Producto
            {
                Nombre = "Bolsa administrativa", Marca = "Interno", Modelo = "BOLSA-TEST",
                TipoInventario = TipoInventario.InsumoAdministrativo, Cantidad = 5, Costo = 10m, Precio = 10m, Activo = true
            };
            producto.Variantes.Add(new ProductoVariante
            {
                Sku = "INS-TEST-001", Cantidad = 5, Costo = 10m, Precio = 10m,
                UmbralStockBajo = 1, Activo = true, EsTecnica = true
            });
            setup.Productos.Add(producto);
            await setup.SaveChangesAsync();
            productoId = producto.Id;
            varianteId = producto.Variantes.Single().Id;
        }

        await using var context = new AppDbContext(options);
        try
        {
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.UsuarioId).Returns(1);
            currentUser.SetupGet(x => x.NombreUsuario).Returns("insumos.integration");
            var auditoria = new Mock<IAuditoriaService>();
            var usuarioScope = new Mock<IUsuarioScopeService>();
            usuarioScope.Setup(x => x.ObtenerActualAsync())
                .ReturnsAsync(new UsuarioScopeActual(1, 1, "Administrador", true));

            var productoRepository = new ProductoRepository(context);
            var varianteRepository = new ProductoVarianteRepository(context);
            var inventario = new InventarioConcurrencyService(context, productoRepository, varianteRepository);
            var service = new ConsumoInsumoService(
                new ConsumoInsumoRepository(context),
                productoRepository,
                varianteRepository,
                new MovimientoInventarioRepository(context, usuarioScope.Object),
                inventario,
                new UnitOfWork(context),
                currentUser.Object,
                auditoria.Object);

            var borrador = await service.CreateAsync(new CreateConsumoInsumoDto
            {
                AreaDestino = "Empaque",
                Motivo = "Preparación de pedidos",
                Detalles = new List<ConsumoInsumoDetalleInputDto>
                {
                    new() { ProductoId = productoId, ProductoVarianteId = varianteId, Cantidad = 2 }
                }
            });

            var confirmado = await service.ConfirmarAsync(borrador.Id);
            Assert.NotNull(confirmado);
            Assert.Equal("Confirmado", confirmado!.Estado);

            context.ChangeTracker.Clear();
            var productoConfirmado = await context.Productos.IgnoreQueryFilters().SingleAsync(p => p.Id == productoId);
            var varianteConfirmada = await context.ProductoVariantes.IgnoreQueryFilters().SingleAsync(v => v.Id == varianteId);
            Assert.Equal(3, productoConfirmado.Cantidad);
            Assert.Equal(3, varianteConfirmada.Cantidad);
            Assert.Equal(0, await context.MovimientosFinancieros.CountAsync());

            var movimientoConfirmacion = await context.MovimientosInventario.SingleAsync(
                m => m.ConsumoInsumoId == borrador.Id && m.Causa == CausaMovimientoInventario.ConsumoAdministrativo);
            Assert.Null(movimientoConfirmacion.CompraId);
            Assert.Null(movimientoConfirmacion.VentaId);
            Assert.Equal("ConsumoInsumo", movimientoConfirmacion.ReferenciaTipo);
            Assert.Equal(borrador.Id, movimientoConfirmacion.ReferenciaId);

            await Assert.ThrowsAsync<BusinessRuleException>(() => service.ConfirmarAsync(borrador.Id));

            var anulado = await service.AnularAsync(borrador.Id, "Consumo revertido por prueba");
            Assert.NotNull(anulado);
            Assert.Equal("Anulado", anulado!.Estado);

            context.ChangeTracker.Clear();
            var productoAnulado = await context.Productos.IgnoreQueryFilters().SingleAsync(p => p.Id == productoId);
            var varianteAnulada = await context.ProductoVariantes.IgnoreQueryFilters().SingleAsync(v => v.Id == varianteId);
            Assert.Equal(5, productoAnulado.Cantidad);
            Assert.Equal(5, varianteAnulada.Cantidad);
            Assert.Equal(0, await context.MovimientosFinancieros.CountAsync());

            var movimientoReversion = await context.MovimientosInventario.SingleAsync(
                m => m.ConsumoInsumoId == borrador.Id && m.Causa == CausaMovimientoInventario.ReversionConsumo);
            Assert.Null(movimientoReversion.CompraId);
            Assert.Null(movimientoReversion.VentaId);
            Assert.Equal("ConsumoInsumo", movimientoReversion.ReferenciaTipo);
            Assert.Equal(borrador.Id, movimientoReversion.ReferenciaId);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
