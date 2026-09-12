using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Security;
using InventoryApp.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace InventoryApp.Tests.Application.Storage;

public class CloudinaryTenantOwnershipTests
{
    [Fact]
    public async Task DeleteAsync_PublicIdDeOtraEmpresa_FallaCerradoAntesDelProveedor()
    {
        var storage = CrearStorage();
        var tenant = CrearScope(usuarioId: 7, empresaId: 31, rolId: 4);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            storage.DeleteAsync(
                tenant,
                "inventoryapp/productos/empresas/32/producto-otro-tenant",
                CancellationToken.None));

        Assert.Contains("no pertenece", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DownloadAsync_UrlDeOtraEmpresa_FallaCerradoAntesDeHttp()
    {
        var storage = CrearStorage();
        var tenant = CrearScope(usuarioId: 7, empresaId: 31, rolId: 4);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            storage.DownloadAsync(
                tenant,
                "https://res.cloudinary.com/test/image/upload/v1/inventoryapp/productos/empresas/32/producto.jpg",
                CancellationToken.None));

        Assert.Contains("no pertenece", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("http://res.cloudinary.com/test/image/upload/v1/inventoryapp/productos/empresas/31/a.jpg")]
    [InlineData("not-a-url")]
    public async Task DownloadAsync_LocatorNoHttpsOMalformado_FallaCerrado(string locator)
    {
        var storage = CrearStorage();
        var tenant = CrearScope(usuarioId: 7, empresaId: 31, rolId: 4);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            storage.DownloadAsync(tenant, locator, CancellationToken.None));
    }

    private static CloudinaryImageStorageService CrearStorage()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cloudinary:CloudName"] = "unit-test",
                ["Cloudinary:ApiKey"] = "unit-test-key",
                ["Cloudinary:ApiSecret"] = "unit-test-secret"
            })
            .Build();

        return new CloudinaryImageStorageService(configuration);
    }

    private static StorageTenantContext CrearScope(int usuarioId, int empresaId, int rolId)
    {
        var membresia = new UsuarioEmpresa(usuarioId, empresaId, rolId);
        var tenant = ContextoTenantActual.DesdeMembresia(membresia, usuarioId, empresaId);
        return StorageTenantContext.Desde(tenant);
    }
}
