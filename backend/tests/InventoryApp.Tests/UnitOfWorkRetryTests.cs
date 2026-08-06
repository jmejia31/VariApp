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
}
