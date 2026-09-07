using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace InventoryApp.Application.DTOs;

public class UpdateProductoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    // Nullable para que clientes antiguos que no envían el campo conserven
    // la clasificación actual del producto.
    public TipoInventario? TipoInventario { get; set; }
    public int Cantidad { get; set; }
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public int UmbralStockBajo { get; set; }
    public int? CategoriaId { get; set; }
    public int? ColorId { get; set; }
    public int? TallaId { get; set; }
    public int? MarcaId { get; set; }
    public int? ModeloId { get; set; }

    /// <summary>
    /// Lista completa de variantes visibles en el formulario. Las variantes
    /// existentes se actualizan y las nuevas se crean dentro de una transacción.
    /// </summary>
    public List<ProductoVarianteFormularioDto> Variantes { get; set; } = new();

    /// Nuevas imágenes a agregar (respetando el máximo de 5 en total).
    public List<IFormFile>? ImagenesNuevas { get; set; }

    /// Ids de ProductoImagen existentes a eliminar.
    public List<int>? ImagenesAEliminarIds { get; set; }

    /// Id de una imagen existente a marcar como principal.
    public int? ImagenPrincipalId { get; set; }
}
