using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace InventoryApp.Infrastructure.Services;

public class CloudinaryImageStorageService : IImageStorageService
{
    private readonly Cloudinary _cloudinary;
    private const string Folder = "inventoryapp/productos";

    public CloudinaryImageStorageService(IConfiguration configuration)
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
                "Cloudinary no está configurado. Revisa Cloudinary:CloudName, Cloudinary:ApiKey y Cloudinary:ApiSecret.");
        }

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<(string Url, string PublicId)> UploadAsync(IFormFile file)
    {
        try
        {
            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = Folder,
                Transformation = new Transformation().Width(800).Height(800).Crop("limit").Quality("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error is not null)
            {
                throw new BusinessRuleException($"No se pudo subir la imagen a Cloudinary: {result.Error.Message}");
            }

            return (result.SecureUrl.ToString(), result.PublicId);
        }
        catch (BusinessRuleException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessRuleException(
                $"No se pudo subir la imagen a Cloudinary. Verifica las credenciales y permisos de Cloudinary. Detalle: {ex.Message}");
        }
    }

    public async Task DeleteAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deleteParams);
    }

    public async Task<(Stream Contenido, string ContentType)?> DownloadAsync(string url)
    {
        // Streaming server-side en vez de redirigir a la URL de Cloudinary
        // directamente (sección 11/12): el backend controla la autorización
        // real de la descarga y puede nombrar el archivo de forma amigable,
        // en vez de confiar en que la URL sea "secreta" por sí sola.
        using var httpClient = new HttpClient();
        try
        {
            var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode) return null;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var stream = await response.Content.ReadAsStreamAsync();
            return (stream, contentType);
        }
        catch
        {
            return null;
        }
    }
}
