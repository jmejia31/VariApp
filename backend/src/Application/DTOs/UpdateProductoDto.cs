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
    public IFormFile? Imagen { get; set; }       // si viene, reemplaza la actual
    public bool EliminarImagen { get; set; }     // si es true y no viene Imagen, borra la actual
}
