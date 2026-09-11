using Microsoft.AspNetCore.Http;

namespace InventoryApp.Application.Interfaces;

public record DocumentoAlmacenado(
    string Url,
    string PublicId,
    string ResourceType,
    string ContentType,
    long SizeBytes);

public interface ICompraDocumentoStorageService
{
    Task<DocumentoAlmacenado> UploadAsync(IFormFile archivo);
    Task DeleteAsync(string publicId, string resourceType);
    Task<(Stream Contenido, string ContentType)?> DownloadAsync(string url);
}
