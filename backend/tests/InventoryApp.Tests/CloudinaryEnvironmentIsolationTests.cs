using InventoryApp.Application.Exceptions;
using InventoryApp.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace InventoryApp.Tests;

public class CloudinaryEnvironmentIsolationTests
{
    private static IConfiguration CrearConfiguracionDesarrollo()
    {
        var valores = new Dictionary<string, string?>
        {
            ["Cloudinary:CloudName"] = "variapp-test",
            ["Cloudinary:ApiKey"] = "test-key",
            ["Cloudinary:ApiSecret"] = "test-secret",
            ["Cloudinary:EnvironmentPrefix"] = "varistorehn_desarrollo"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(valores)
            .Build();
    }

    [Fact]
    public async Task Productos_DesarrolloNoPuedeEliminarImagenProductiva()
    {
        var service = new CloudinaryImageStorageService(CrearConfiguracionDesarrollo());

        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.DeleteAsync("inventoryapp/productos/producto-productivo"));

        Assert.Contains("Producción", excepcion.Message);
    }

    [Fact]
    public async Task Compras_DesarrolloNoPuedeEliminarComprobanteProductivo()
    {
        var service = new CloudinaryCompraDocumentoStorageService(CrearConfiguracionDesarrollo());

        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.DeleteAsync("inventoryapp/compras/comprobante-productivo", "raw"));

        Assert.Contains("Producción", excepcion.Message);
    }
}
