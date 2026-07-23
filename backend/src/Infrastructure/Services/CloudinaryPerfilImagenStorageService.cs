using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InventoryApp.Infrastructure.Services;

public class CloudinaryPerfilImagenStorageService : IPerfilImagenStorageService
{
    private const string Folder = "variapp/perfiles";
    private const long MaximoBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryPerfilImagenStorageService> _logger;

    public CloudinaryPerfilImagenStorageService(
        IConfiguration configuration,
        ILogger<CloudinaryPerfilImagenStorageService> logger)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret) ||
            cloudName == "CHANGE_ME" || apiKey == "CHANGE_ME" || apiSecret == "CHANGE_ME")
        {
            throw new BusinessRuleException("Cloudinary no está configurado para almacenar fotografías de perfil.");
        }

        _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        _cloudinary.Api.Secure = true;
        _logger = logger;
    }

    public async Task<(string Url, string PublicId)> UploadAsync(IFormFile foto)
    {
        ValidarFoto(foto);

        try
        {
            await using var stream = foto.OpenReadStream();
            var nombreSeguro = $"perfil-{Guid.NewGuid():N}{Path.GetExtension(foto.FileName).ToLowerInvariant()}";
            var parametros = new ImageUploadParams
            {
                File = new FileDescription(nombreSeguro, stream),
                Folder = Folder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false,
                Transformation = new Transformation()
                    .Width(512)
                    .Height(512)
                    .Crop("fill")
                    .Gravity("auto")
                    .Quality("auto")
            };

            var resultado = await _cloudinary.UploadAsync(parametros);
            if (resultado.Error is not null || resultado.SecureUrl is null || string.IsNullOrWhiteSpace(resultado.PublicId))
            {
                _logger.LogWarning("Cloudinary rechazó una foto de perfil. Error={Error}", resultado.Error?.Message);
                throw new BusinessRuleException("No fue posible guardar la fotografía de perfil.");
            }

            return (resultado.SecureUrl.ToString(), resultado.PublicId);
        }
        catch (BusinessRuleException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir una fotografía de perfil a Cloudinary.");
            throw new BusinessRuleException("No fue posible guardar la fotografía de perfil. Intenta nuevamente.");
        }
    }

    public async Task DeleteAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId)) return;

        try
        {
            var resultado = await _cloudinary.DestroyAsync(new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image,
                Invalidate = true
            });

            if (!string.Equals(resultado.Result, "ok", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(resultado.Result, "not found", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Cloudinary no confirmó la eliminación de {PublicId}. Resultado={Resultado}", publicId, resultado.Result);
            }
        }
        catch (Exception ex)
        {
            // La referencia de base de datos no debe restaurarse porque falló una
            // limpieza remota; el incidente queda registrado para conciliación.
            _logger.LogError(ex, "No fue posible eliminar la foto de perfil {PublicId} de Cloudinary.", publicId);
        }
    }

    private static void ValidarFoto(IFormFile foto)
    {
        if (foto is null || foto.Length == 0)
            throw new BusinessRuleException("Selecciona una fotografía válida.");

        if (foto.Length > MaximoBytes)
            throw new BusinessRuleException("La fotografía no puede superar 5 MB.");

        var extension = Path.GetExtension(foto.FileName);
        if (!TiposPermitidos.Contains(foto.ContentType) || !ExtensionesPermitidas.Contains(extension))
            throw new BusinessRuleException("La fotografía debe ser JPG, PNG o WebP.");
    }
}
