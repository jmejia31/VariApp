using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class SecuenciaDocumentoServiceTests
{
    [Fact]
    public async Task ObtenerAsync_DevuelveSecuenciaDelTenantYFormatoActual()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n66d-{Guid.NewGuid():N}")
            .Options;

        await using var db = new AppDbContext(options);
        db.SecuenciasDocumento.Add(new SecuenciaDocumento(1, null, "factura", "FAC-", 6, 41));
        await db.SaveChangesAsync();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(10, 1, 20, "Administrador", true));

        var service = new SecuenciaDocumentoService(db, scope.Object);
        var result = await service.ObtenerAsync(1, null, " factura ");

        Assert.Equal(1, result.EmpresaId);
        Assert.Equal("FACTURA", result.TipoDocumento);
        Assert.Equal(41, result.UltimoValor);
        Assert.Equal("FAC-000041", result.UltimoNumeroFormateado);
    }

    [Fact]
    public async Task ObtenerAsync_FallaCerradoSinMembresiaTenantActiva()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n66d-denied-{Guid.NewGuid():N}")
            .Options;

        await using var db = new AppDbContext(options);
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);

        var service = new SecuenciaDocumentoService(db, scope.Object);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.ObtenerAsync(2, null, "FACTURA"));

        Assert.Contains("no tiene acceso", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
