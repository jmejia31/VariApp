using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace InventoryApp.Infrastructure.Services;

public class CloudinaryCompraDocumentoStorageService : ICompraDocumentoStorageService
{
    private const string BaseFolder = "inventoryapp/compras";
    private const long MaxDownloadBytes = 10 * 1024 * 1024;
    private readonly Cloudinary _cloudinary;
    private readonly string _cloudName;
    private readonly string _folder;
    private readonly string? _environmentPrefix;

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

        _cloudName = cloudName.Trim();
        _cloudinary = new Cloudinary(new Account(_cloudName, apiKey, apiSecret));
        _cloudinary.Api.Secure = true;
        _folder = CloudinaryFolderResolver.Resolve(configuration, BaseFolder);
        _environmentPrefix = CloudinaryFolderResolver.GetEnvironmentPrefix(configuration);
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
                    Folder = _folder,
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
                Folder = _folder,
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
        if (!CloudinaryFolderResolver.CanDelete(_environmentPrefix, publicId))
        {
            throw new BusinessRuleException(
                "El entorno de Desarrollo no puede eliminar un comprobante que pertenece a Producción.");
        }

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
        if (!EsUrlCloudinaryPermitida(url))
            return null;

        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        try
        {
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
                return null;

            if (response.Content.Headers.ContentLength is long contentLength &&
                contentLength > MaxDownloadBytes)
            {
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType
                ?? "application/octet-stream";
            await using var remote = await response.Content.ReadAsStreamAsync();
            var contenido = new MemoryStream();
            var buffer = new byte[81920];
            long total = 0;
            while (true)
            {
                var read = await remote.ReadAsync(buffer.AsMemory(0, buffer.Length));
                if (read == 0)
                    break;

                total += read;
                if (total > MaxDownloadBytes)
                {
                    await contenido.DisposeAsync();
                    return null;
                }

                await contenido.WriteAsync(buffer.AsMemory(0, read));
            }

            contenido.Position = 0;
            return (contenido, contentType);
        }
        catch
        {
            return null;
        }
    }

    private bool EsUrlCloudinaryPermitida(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase) ||
            !uri.IsDefaultPort ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return segments.Length >= 3 &&
               string.Equals(Uri.UnescapeDataString(segments[0]), _cloudName, StringComparison.Ordinal) &&
               (string.Equals(segments[1], "image", StringComparison.Ordinal) ||
                string.Equals(segments[1], "raw", StringComparison.Ordinal)) &&
               string.Equals(segments[2], "upload", StringComparison.Ordinal);
    }
}
