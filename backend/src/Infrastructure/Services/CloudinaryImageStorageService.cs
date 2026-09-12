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
    private readonly string _cloudName;
    private readonly string _folder;
    private readonly string? _environmentPrefix;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IUsuarioScopeService? _usuarioScopeService;
    private const string BaseFolder = "inventoryapp/productos";

    public CloudinaryImageStorageService(
        IConfiguration configuration,
        IHttpContextAccessor? httpContextAccessor = null,
        IUsuarioScopeService? usuarioScopeService = null)
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

        _cloudName = cloudName.Trim();
        var account = new Account(_cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
        _folder = CloudinaryFolderResolver.Resolve(configuration, BaseFolder);
        _environmentPrefix = CloudinaryFolderResolver.GetEnvironmentPrefix(configuration);
        _httpContextAccessor = httpContextAccessor;
        _usuarioScopeService = usuarioScopeService;
    }

    public async Task<(string Url, string PublicId)> UploadAsync(IFormFile file)
    {
        var tenant = await ResolverTenantActualAsync();
        return await UploadAsync(tenant, file, RequestAborted);
    }

    public Task<(string Url, string PublicId)> UploadAsync(
        StorageTenantContext tenant,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(file);
        return UploadInternalAsync(file, TenantFolder(tenant), cancellationToken);
    }

    private async Task<(string Url, string PublicId)> UploadInternalAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var segura = await ImagenUploadSecurity.ProcesarAsync(file);
            cancellationToken.ThrowIfCancellationRequested();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(segura.NombreArchivo, segura.Contenido),
                Folder = folder,
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
        catch (OperationCanceledException)
        {
            throw;
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

        var tenant = await ResolverTenantActualAsync();
        await DeleteAsync(tenant, publicId, RequestAborted);
    }

    public async Task DeleteAsync(
        StorageTenantContext tenant,
        string publicId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        if (string.IsNullOrWhiteSpace(publicId))
            throw new ArgumentException("El publicId es obligatorio.", nameof(publicId));

        ExigirLocatorTenant(tenant, publicId, esUrl: false);
        cancellationToken.ThrowIfCancellationRequested();
        await DeleteInternalAsync(publicId);
    }

    private async Task DeleteInternalAsync(string publicId)
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
        var tenant = await ResolverTenantActualAsync();
        return await DownloadAsync(tenant, url, RequestAborted);
    }

    public Task<(Stream Contenido, string ContentType)?> DownloadAsync(
        StorageTenantContext tenant,
        string url,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("La URL de almacenamiento es obligatoria.", nameof(url));

        ExigirLocatorTenant(tenant, url, esUrl: true);
        return DownloadInternalAsync(url, cancellationToken);
    }

    private static async Task<(Stream Contenido, string ContentType)?> DownloadInternalAsync(
        string url,
        CancellationToken cancellationToken)
    {
        // Streaming server-side en vez de redirigir a la URL de Cloudinary
        // directamente: el backend controla la autorización real de la descarga.
        using var httpClient = new HttpClient();
        try
        {
            var response = await httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return (stream, contentType);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private CancellationToken RequestAborted =>
        _httpContextAccessor?.HttpContext?.RequestAborted ?? default;

    private Task<StorageTenantContext> ResolverTenantActualAsync() =>
        StorageTenantContextResolver.ResolverRequeridoAsync(
            _httpContextAccessor,
            _usuarioScopeService,
            RequestAborted);

    private string TenantFolder(StorageTenantContext tenant) =>
        $"{_folder.TrimEnd('/')}/{tenant.TenantPrefix}";

    private void ExigirLocatorTenant(StorageTenantContext tenant, string locator, bool esUrl)
    {
        var tenantFolder = TenantFolder(tenant).Trim('/');

        if (!esUrl)
        {
            var normalizado = locator.Trim().Trim('/');
            if (!normalizado.StartsWith($"{tenantFolder}/", StringComparison.Ordinal))
            {
                throw new BusinessRuleException(
                    "El recurso solicitado no pertenece al contexto tenant verificado.");
            }

            return;
        }

        if (!Uri.TryCreate(locator, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase) ||
            !uri.IsDefaultPort ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new BusinessRuleException(
                "La URL de almacenamiento no es un locator seguro para este tenant.");
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 4 ||
            !string.Equals(Uri.UnescapeDataString(segments[0]), _cloudName, StringComparison.Ordinal) ||
            !string.Equals(segments[1], "image", StringComparison.Ordinal) ||
            !string.Equals(segments[2], "upload", StringComparison.Ordinal))
        {
            throw new BusinessRuleException(
                "La URL de almacenamiento no pertenece al origen Cloudinary configurado.");
        }

        var marker = $"/{tenantFolder}/";
        if (!uri.AbsolutePath.Contains(marker, StringComparison.Ordinal))
        {
            throw new BusinessRuleException(
                "El recurso solicitado no pertenece al contexto tenant verificado.");
        }
    }
}
