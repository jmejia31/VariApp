namespace InventoryApp.Application.DTOs;

public class ProductoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Cantidad { get; set; }
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public int UmbralStockBajo { get; set; }
    public bool TieneStockBajo { get; set; }
    public bool Activo { get; set; }

    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }

    public string? ImagenPrincipalUrl { get; set; }
    public List<ProductoImagenDto> Imagenes { get; set; } = new();
    public int TotalImagenes { get; set; }

    public int? CreadoPorUsuarioId { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }
    public string? ActualizadoPorNombreUsuario { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}
