using Microsoft.AspNetCore.Http;

namespace InventoryApp.Application.Validators;

public static class ImagenValidationHelper
{
    public const int MaxImagenes = 5;
    public const long MaxBytesPorImagen = 5 * 1024 * 1024; // 5 MB

    private static readonly string[] TiposPermitidos = { "image/jpeg", "image/png", "image/webp" };

    public static bool EsImagenValida(IFormFile archivo) =>
        TiposPermitidos.Contains(archivo.ContentType?.ToLower()) && archivo.Length > 0 && archivo.Length <= MaxBytesPorImagen;
}
