using Microsoft.AspNetCore.Http;

namespace InventoryApp.Application.Interfaces;

public interface IImageStorageService
{
    /// Sube la imagen a Cloudinary y retorna (Url, PublicId)
    Task<(string Url, string PublicId)> UploadAsync(IFormFile file);

    /// Elimina la imagen de Cloudinary usando su PublicId
    Task DeleteAsync(string publicId);
}
