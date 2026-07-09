using Microsoft.AspNetCore.Http;

namespace InventoryApp.Application.DTOs;

public class UpdateProductoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Cantidad { get; set; }
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public int UmbralStockBajo { get; set; }
    public int? CategoriaId { get; set; }

    /// Nuevas imágenes a agregar (respetando el máximo de 5 en total).
    public List<IFormFile>? ImagenesNuevas { get; set; }

    /// Ids de ProductoImagen existentes a eliminar.
    public List<int>? ImagenesAEliminarIds { get; set; }

    /// Id de una imagen existente a marcar como principal.
    public int? ImagenPrincipalId { get; set; }
}
