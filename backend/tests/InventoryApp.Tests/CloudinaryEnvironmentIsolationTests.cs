using InventoryApp.Application.Exceptions;
using InventoryApp.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace InventoryApp.Tests;

public class CloudinaryEnvironmentIsolationTests
{
    private static IConfiguration CrearConfiguracionDev()
    {
        var valores = new Dictionary<string, string?>
        {
            ["Cloudinary:CloudName"] = "solqaryn-test",
            ["Cloudinary:ApiKey"] = "test-key",
            ["Cloudinary:ApiSecret"] = "test-secret",
            ["Cloudinary:EnvironmentPrefix"] = "solqaryn_dev"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(valores)
            .Build();
    }

    [Fact]
    public async Task Productos_DevNoPuedeEliminarImagenProductiva()
    {
        var service = new CloudinaryImageStorageService(CrearConfiguracionDev());

        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.DeleteAsync("inventoryapp/productos/producto-productivo"));

        Assert.Contains("Producción", excepcion.Message);
    }

    [Fact]
    public async Task Compras_DevNoPuedeEliminarComprobanteProductivo()
    {
        var service = new CloudinaryCompraDocumentoStorageService(CrearConfiguracionDev());

        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.DeleteAsync("inventoryapp/compras/comprobante-productivo", "raw"));

        Assert.Contains("Producción", excepcion.Message);
    }
}
