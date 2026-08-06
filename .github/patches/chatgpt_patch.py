from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
path = ROOT / "backend/tests/InventoryApp.Tests/CargaMasivaConcurrencyTests.cs"
path.write_text(r'''using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

[Trait("Category", "Integration")]
public class CargaMasivaConcurrencyTests
{
    private sealed record Escenario(
        DbContextOptions<AppDbContext> Options,
        int CargaId,
        int ProductoId,
        int VarianteId);

    private static DbContextOptions<AppDbContext> CrearOpciones(string nombreBase) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                $"Server=localhost;Port=3306;Database={nombreBase};User=root;Password=root;",
                new MySqlServerVersion(new Version(8, 4, 0)))
            .Options;

    private static Mock<ICurrentUserService> CrearUsuarioActual()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns(1);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("integration-admin");
        currentUser.SetupGet(x => x.NombreCompleto).Returns("Integration Admin");
        currentUser.SetupGet(x => x.EsAdministrador).Returns(true);
        return currentUser;
    }

    private static CargaMasivaService CrearServicio(AppDbContext context)
    {
        return new CargaMasivaService(
            context,
            CrearUsuarioActual().Object,
            Mock.Of<IAuditoriaService>(),
            Mock.Of<ILogger<CargaMasivaService>>(),
            Mock.Of<ITipoClientePredeterminadoResolver>());
    }

    private static async Task<Escenario> PrepararAsync(
        int cantidadActual,
        int cantidadNueva)
    {
        var nombreBase = $"test_carga_snapshot_{Guid.NewGuid():N}";
        var options = CrearOpciones(nombreBase);

        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();

        var color = new CatalogoProducto
        {
            Tipo = TipoCatalogoProducto.Color,
            Nombre = "Negro",
            CodigoVisual = "#111111",
            Activo = true,
            Eliminado = false,
            CreadoPorUsuarioId = 1,
            CreadoPorNombreUsuario = "integration-admin"
        };
        context.CatalogosProducto.Add(color);
        await context.SaveChangesAsync();

        var producto = new Producto
        {
            Nombre = "Producto carga concurrente",
            Marca = "Marca Test",
            Modelo = "Modelo Test",
            Cantidad = cantidadActual,
            Costo = 10m,
            Precio = 20m,
            UmbralStockBajo = 1,
            Activo = true,
            Eliminado = false,
            CreadoPorUsuarioId = 1,
            CreadoPorNombreUsuario = "integration-admin"
        };
        context.Productos.Add(producto);
        await context.SaveChangesAsync();

        var variante = new ProductoVariante
        {
            ProductoId = producto.Id,
            ColorId = color.Id,
            Sku = $"SKU-{Guid.NewGuid():N}".ToUpperInvariant(),
            CodigoBarras = Guid.NewGuid().ToString("N"),
            Cantidad = cantidadActual,
            UmbralStockBajo = 1,
            Costo = 10m,
            Precio = 20m,
            Activo = true,
            Eliminado = false,
            CreadoPorUsuarioId = 1,
            CreadoPorNombreUsuario = "integration-admin"
        };
        context.ProductoVariantes.Add(variante);
        await context.SaveChangesAsync();

        var fila = new CargaMasivaFilaDto
        {
            NumeroFila = 2,
            Accion = "Actualizar",
            EsValida = true,
            ProductoIdSnapshot = producto.Id,
            ProductoVarianteIdSnapshot = variante.Id,
            CantidadActualSnapshot = cantidadActual,
            FechaValidacionSnapshot = DateTime.UtcNow,
            Datos = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Producto"] = producto.Nombre,
                ["Marca"] = producto.Marca,
                ["Modelo"] = producto.Modelo,
                ["Color"] = color.Nombre,
                ["SKU"] = variante.Sku,
                ["CodigoBarras"] = variante.CodigoBarras,
                ["Cantidad"] = cantidadNueva.ToString(),
                ["UmbralStockBajo"] = "1",
                ["Costo"] = "12.50",
                ["Precio"] = "25.00",
                ["Activo"] = "true"
            }
        };

        var carga = new CargaMasiva
        {
            Tipo = TipoCargaMasiva.VariantesInventario,
            Estado = EstadoCargaMasiva.Validada,
            NombreArchivo = "variantes.csv",
            Extension = ".csv",
            ContentType = "text/csv",
            TamanoBytes = 128,
            HashArchivo = Guid.NewGuid().ToString("N").PadRight(64, '0'),
            DatosNormalizadosJson = JsonSerializer.Serialize(
                new[] { fila },
                new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            TotalFilas = 1,
            FilasValidas = 1,
            FilasConError = 0,
            FechaValidacion = DateTime.UtcNow,
            CreadoPorUsuarioId = 1,
            CreadoPorNombreUsuario = "integration-admin"
        };
        context.CargasMasivas.Add(carga);
        await context.SaveChangesAsync();

        return new Escenario(options, carga.Id, producto.Id, variante.Id);
    }

    private static async Task LimpiarAsync(DbContextOptions<AppDbContext> options)
    {
        await using var context = new AppDbContext(options);
        await context.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task ConfirmarAsync_SnapshotVigente_AplicaAjusteYConfirmaCarga()
    {
        var escenario = await PrepararAsync(cantidadActual: 5, cantidadNueva: 9);
        try
        {
            await using (var context = new AppDbContext(escenario.Options))
            {
                var service = CrearServicio(context);
                var resultado = await service.ConfirmarAsync(escenario.CargaId);
                Assert.Equal(nameof(EstadoCargaMasiva.Confirmada), resultado.Estado);
            }

            await using var verify = new AppDbContext(escenario.Options);
            var variante = await verify.ProductoVariantes
                .AsNoTracking()
                .SingleAsync(x => x.Id == escenario.VarianteId);
            var producto = await verify.Productos
                .AsNoTracking()
                .SingleAsync(x => x.Id == escenario.ProductoId);
            var carga = await verify.CargasMasivas
                .AsNoTracking()
                .SingleAsync(x => x.Id == escenario.CargaId);
            var movimientos = await verify.MovimientosInventario
                .AsNoTracking()
                .Where(x => x.ReferenciaTipo == "CargaMasiva" && x.ReferenciaId == escenario.CargaId)
                .ToListAsync();

            Assert.Equal(9, variante.Cantidad);
            Assert.Equal(9, producto.Cantidad);
            Assert.Equal(EstadoCargaMasiva.Confirmada, carga.Estado);
            var movimiento = Assert.Single(movimientos);
            Assert.Equal(TipoMovimientoInventario.Ajuste, movimiento.Tipo);
            Assert.Equal(5, movimiento.StockAnterior);
            Assert.Equal(9, movimiento.StockNuevo);
            Assert.Equal(4, movimiento.Cantidad);
        }
        finally
        {
            await LimpiarAsync(escenario.Options);
        }
    }

    [Fact]
    public async Task ConfirmarAsync_SnapshotVencido_RevierteLoteYConservaStockConcurrente()
    {
        var escenario = await PrepararAsync(cantidadActual: 5, cantidadNueva: 9);
        try
        {
            await using (var concurrente = new AppDbContext(escenario.Options))
            {
                var variante = await concurrente.ProductoVariantes
                    .SingleAsync(x => x.Id == escenario.VarianteId);
                var producto = await concurrente.Productos
                    .SingleAsync(x => x.Id == escenario.ProductoId);
                variante.Cantidad = 4;
                producto.Cantidad = 4;
                await concurrente.SaveChangesAsync();
            }

            await using (var context = new AppDbContext(escenario.Options))
            {
                var service = CrearServicio(context);
                var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
                    service.ConfirmarAsync(escenario.CargaId));
                Assert.Contains("cambió", ex.Message, StringComparison.OrdinalIgnoreCase);
            }

            await using var verify = new AppDbContext(escenario.Options);
            var varianteFinal = await verify.ProductoVariantes
                .AsNoTracking()
                .SingleAsync(x => x.Id == escenario.VarianteId);
            var productoFinal = await verify.Productos
                .AsNoTracking()
                .SingleAsync(x => x.Id == escenario.ProductoId);
            var cargaFinal = await verify.CargasMasivas
                .AsNoTracking()
                .SingleAsync(x => x.Id == escenario.CargaId);
            var movimientos = await verify.MovimientosInventario
                .AsNoTracking()
                .CountAsync(x => x.ReferenciaTipo == "CargaMasiva" && x.ReferenciaId == escenario.CargaId);

            Assert.Equal(4, varianteFinal.Cantidad);
            Assert.Equal(4, productoFinal.Cantidad);
            Assert.Equal(EstadoCargaMasiva.Fallida, cargaFinal.Estado);
            Assert.Contains("Revalida", cargaFinal.ErrorGeneral ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, movimientos);
        }
        finally
        {
            await LimpiarAsync(escenario.Options);
        }
    }
}
''', encoding="utf-8")

print("Pruebas MySQL de snapshots de cargas masivas agregadas.")
