using Microsoft.AspNetCore.Http;

namespace InventoryApp.Application.DTOs;

public class CreateProductoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Cantidad { get; set; }
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public int UmbralStockBajo { get; set; } = 5;
    public int? CategoriaId { get; set; }

    /// Máximo 5 imágenes. La primera se marca como principal.
    public List<IFormFile>? Imagenes { get; set; }
}
