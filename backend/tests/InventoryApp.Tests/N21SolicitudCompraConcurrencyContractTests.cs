using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N21SolicitudCompraConcurrencyContractTests
{
    [Fact]
    public async Task Lectura_for_update_exige_transaccion_activa()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n21-solicitud-lock-{Guid.NewGuid():N}")
            .Options;
        await using var context = new AppDbContext(options);
        var repository = new SolicitudCompraRepository(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.GetByIdForUpdateAsync(17));

        Assert.Contains("transacción activa", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
