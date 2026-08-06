using System;
using System.Threading.Tasks;
using InventoryApp.Application.Exceptions;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryApp.Tests;

public class UnitOfWorkRetryTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_EjecutaOperacionExitosamente()
    {
        await using var context = CreateInMemoryContext();
        var uow = new UnitOfWork(context);

        var executed = false;
        await uow.ExecuteInTransactionAsync(() =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        Assert.True(executed);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ErrorNoTransitorio_LanzaExcepcionDirectamente()
    {
        await using var context = CreateInMemoryContext();
        var uow = new UnitOfWork(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.ExecuteInTransactionAsync(() => throw new InvalidOperationException("Non-transient failure")));

        Assert.Equal("Non-transient failure", ex.Message);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_PreservaExcepcionOriginalNoTraducida()
    {
        await using var context = CreateInMemoryContext();
        var uow = new UnitOfWork(context);

        var originalEx = new InvalidOperationException("Test original trace preservation");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.ExecuteInTransactionAsync(() => throw originalEx));

        Assert.Same(originalEx, thrown);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ReintentaErrorTransitorio1205_YSucedeEnSegundoIntento()
    {
        await using var context = CreateInMemoryContext();
        var uow = new UnitOfWork(context);

        int attempts = 0;
        await uow.ExecuteInTransactionAsync(() =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new Exception("Error 1205: Lock wait timeout exceeded; try restarting transaction");
            }
            return Task.CompletedTask;
        });

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ReintentaErrorTransitorio1213_YSucedeEnSegundoIntento()
    {
        await using var context = CreateInMemoryContext();
        var uow = new UnitOfWork(context);

        int attempts = 0;
        await uow.ExecuteInTransactionAsync(() =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new Exception("Error 1213: Deadlock found when trying to get lock; try restarting transaction");
            }
            return Task.CompletedTask;
        });

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_NoReintentaErrorGenericoOMaximoSuperado()
    {
        await using var context = CreateInMemoryContext();
        var uow = new UnitOfWork(context);

        int attempts = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.ExecuteInTransactionAsync(() =>
            {
                attempts++;
                throw new InvalidOperationException("Standard failure");
            }));

        Assert.Equal(1, attempts);
    }
}
