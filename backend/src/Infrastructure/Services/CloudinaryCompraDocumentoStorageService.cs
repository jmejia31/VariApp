using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace InventoryApp.Infrastructure.Services;

public class CloudinaryCompraDocumentoStorageService : ICompraDocumentoStorageService
{
    private const string Folder = "inventoryapp/compras";
    private readonly Cloudinary _cloudinary;

    public CloudinaryCompraDocumentoStorageService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret) ||
            cloudName == "CHANGE_ME" ||
            apiKey == "CHANGE_ME" ||
            apiSecret == "CHANGE_ME")
        {
            throw new BusinessRuleException(
                "Cloudinary no está configurado para almacenar comprobantes de compras.");
        }

        _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        _cloudinary.Api.Secure = true;
    }

    public async Task<DocumentoAlmacenado> UploadAsync(IFormFile archivo)
    {
        try
        {
            await using var stream = archivo.OpenReadStream();
            var esPdf = string.Equals(archivo.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase);

            if (esPdf)
            {
                var parametros = new RawUploadParams
                {
                    File = new FileDescription(archivo.FileName, stream),
                    Folder = Folder,
                    UseFilename = true,
                    UniqueFilename = true
                };

                var resultado = await _cloudinary.UploadAsync(parametros);
                if (resultado.Error is not null)
                    throw new BusinessRuleException($"Cloudinary rechazó el PDF: {resultado.Error.Message}");

                return new DocumentoAlmacenado(
                    resultado.SecureUrl.ToString(),
                    resultado.PublicId,
                    "raw",
                    archivo.ContentType,
                    archivo.Length);
            }

            var parametrosImagen = new ImageUploadParams
            {
                File = new FileDescription(archivo.FileName, stream),
                Folder = Folder,
                UseFilename = true,
                UniqueFilename = true
            };

            var resultadoImagen = await _cloudinary.UploadAsync(parametrosImagen);
            if (resultadoImagen.Error is not null)
                throw new BusinessRuleException($"Cloudinary rechazó la imagen: {resultadoImagen.Error.Message}");

            return new DocumentoAlmacenado(
                resultadoImagen.SecureUrl.ToString(),
                resultadoImagen.PublicId,
                "image",
                archivo.ContentType,
                archivo.Length);
        }
        catch (BusinessRuleException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessRuleException(
                $"No se pudo almacenar el comprobante en Cloudinary. Detalle: {ex.Message}");
        }
    }

    public async Task DeleteAsync(string publicId, string resourceType)
    {
        var parametros = new DeletionParams(publicId)
        {
            ResourceType = string.Equals(resourceType, "raw", StringComparison.OrdinalIgnoreCase)
                ? ResourceType.Raw
                : ResourceType.Image,
            Invalidate = true
        };

        var resultado = await _cloudinary.DestroyAsync(parametros);
        if (resultado.Error is not null)
            throw new BusinessRuleException($"No se pudo retirar el comprobante de Cloudinary: {resultado.Error.Message}");
    }

    public async Task<(Stream Contenido, string ContentType)?> DownloadAsync(string url)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        try
        {
            var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode) return null;

            var contentType = response.Content.Headers.ContentType?.MediaType
                ?? "application/octet-stream";
            var contenido = await response.Content.ReadAsStreamAsync();
            return (contenido, contentType);
        }
        catch
        {
            return null;
        }
    }
}
