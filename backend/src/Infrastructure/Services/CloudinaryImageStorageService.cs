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
    private readonly string _folder;
    private readonly string? _environmentPrefix;
    private const string BaseFolder = "inventoryapp/productos";

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
        _folder = CloudinaryFolderResolver.Resolve(configuration, BaseFolder);
        _environmentPrefix = CloudinaryFolderResolver.GetEnvironmentPrefix(configuration);
    }

    public async Task<(string Url, string PublicId)> UploadAsync(IFormFile file)
    {
        try
        {
            using var segura = await ImagenUploadSecurity.ProcesarAsync(file);

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(segura.NombreArchivo, segura.Contenido),
                Folder = _folder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false,
                Transformation = new Transformation().Width(800).Height(800).Crop("limit").Quality("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error is not null || result.SecureUrl is null || string.IsNullOrWhiteSpace(result.PublicId))
                throw new BusinessRuleException("No se pudo guardar la imagen del producto.");

            return (result.SecureUrl.ToString(), result.PublicId);
        }
        catch (BusinessRuleException)
        {
            throw;
        }
        catch
        {
            // No se devuelve al cliente el mensaje técnico del proveedor externo,
            // evitando filtrar detalles de configuración o infraestructura.
            throw new BusinessRuleException(
                "No se pudo guardar la imagen del producto. Intenta nuevamente.");
        }
    }

    public async Task DeleteAsync(string publicId)
    {
        if (!CloudinaryFolderResolver.CanDelete(_environmentPrefix, publicId))
        {
            throw new BusinessRuleException(
                "El entorno de Desarrollo no puede eliminar una imagen que pertenece a Producción.");
        }

        var deleteParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deleteParams);
    }

    public async Task<(Stream Contenido, string ContentType)?> DownloadAsync(string url)
    {
        // Streaming server-side en vez de redirigir a la URL de Cloudinary
        // directamente: el backend controla la autorización real de la descarga.
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
