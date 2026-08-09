using InventoryApp.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace InventoryApp.Application.Interfaces;

public interface IProductoVarianteImagenService
{
    Task<IReadOnlyList<ProductoImagenDto>?> GetAsync(int productoId, int varianteId);
    Task<IReadOnlyList<ProductoImagenDto>> AddAsync(int productoId, int varianteId, IReadOnlyCollection<IFormFile> archivos);
    Task<bool> SetPrincipalAsync(int productoId, int varianteId, int imagenId);
    Task<bool> DeleteAsync(int productoId, int varianteId, int imagenId);
}
