using Microsoft.AspNetCore.Http;

namespace InventoryApp.Application.Interfaces;

public interface IImageStorageService
{
    /// Sube la imagen a Cloudinary y retorna (Url, PublicId)
    Task<(string Url, string PublicId)> UploadAsync(IFormFile file);

    /// Elimina la imagen de Cloudinary usando su PublicId
    Task DeleteAsync(string publicId);

    /// Descarga el contenido real de una imagen ya almacenada (sección 11:
    /// "la descarga debe utilizar archivos reales almacenados por el
    /// sistema", nunca simulados). Devuelve null si el recurso no existe o
    /// no se pudo obtener — el llamador decide la respuesta HTTP apropiada.
    Task<(Stream Contenido, string ContentType)?> DownloadAsync(string url);
}
