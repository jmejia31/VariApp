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
using InventoryApp.Infrastructure.Services;
using InventoryApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class TipoClienteConcurrencyTests
{
    private const string ConnectionString = "Server=localhost;Port=3306;Database=inventoryapp_test;User=root;Password=root;";

    private async Task<AppDbContext?> TryGetMySqlContextAsync()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql(ConnectionString, new MySqlServerVersion(new Version(8, 4, 3)))
                .Options;

            var context = new AppDbContext(options);
            // Probar conexión y asegurar base de datos creada
            await context.Database.EnsureCreatedAsync();
            return context;
        }
        catch (Exception)
        {
            // Retornar null si no se puede conectar (evita romper CI si MySQL no está disponible en este paso)
            return null;
        }
    }

    [Fact]
    public async Task Concurrency_MarcarPredeterminado_SoloUnGanador()
    {
        using var context = await TryGetMySqlContextAsync();
        if (context == null)
        {
            // Ignorar la prueba si no hay MySQL disponible
            return;
        }

        try
        {
            // Limpiar datos previos
            context.TipoClientes.RemoveRange(context.TipoClientes);
            await context.SaveChangesAsync();

            // Insertar dos tipos de clientes activos no predeterminados
            var tipoA = new TipoCliente { Codigo = "TIPO_A", Nombre = "Tipo A", ColorHex = "#111111", Activo = true, EsPredeterminado = false };
            var tipoB = new TipoCliente { Codigo = "TIPO_B", Nombre = "Tipo B", ColorHex = "#222222", Activo = true, EsPredeterminado = false };
            context.TipoClientes.AddRange(tipoA, tipoB);
            await context.SaveChangesAsync();

            var currentUserMock = new Mock<ICurrentUserService>();
            currentUserMock.Setup(c => c.UsuarioId).Returns(1);
            currentUserMock.Setup(c => c.NombreUsuario).Returns("test_user");

            var auditoriaMock = new Mock<IAuditoriaService>();

            var uow = new UnitOfWork(context);
            var repo = new TipoClienteRepository(context);
            var service = new TipoClienteService(repo, currentUserMock.Object, auditoriaMock.Object, uow);

            // Intentar marcar concurrentemente ambos como predeterminado
            var updateDtoA = new UpdateTipoClienteDto { Nombre = "Tipo A", ColorHex = "#111111", Activo = true, EsPredeterminado = true };
            var updateDtoB = new UpdateTipoClienteDto { Nombre = "Tipo B", ColorHex = "#222222", Activo = true, EsPredeterminado = true };

            var taskA = service.UpdateAsync(tipoA.Id, updateDtoA);
            var taskB = service.UpdateAsync(tipoB.Id, updateDtoB);

            Exception? exA = null;
            Exception? exB = null;

            try { await taskA; } catch (Exception ex) { exA = ex; }
            try { await taskB; } catch (Exception ex) { exB = ex; }

            // Volver a leer de base de datos
            var dbTipos = await context.TipoClientes.AsNoTracking().ToListAsync();
            var predeterminados = dbTipos.Where(t => t.EsPredeterminado).ToList();

            // Verificar que exactamente uno ganó y el otro lanzó la excepción de negocio controlada
            Assert.Single(predeterminados);

            if (exA != null)
            {
                Assert.IsType<BusinessRuleException>(exA);
                Assert.Contains("Conflicto de concurrencia", exA.Message);
                Assert.Null(exB);
            }
            else
            {
                Assert.NotNull(exB);
                Assert.IsType<BusinessRuleException>(exB);
                Assert.Contains("Conflicto de concurrencia", exB.Message);
            }
        }
        finally
        {
            // Limpieza
            context.TipoClientes.RemoveRange(context.TipoClientes);
            await context.SaveChangesAsync();
        }
    }
}
