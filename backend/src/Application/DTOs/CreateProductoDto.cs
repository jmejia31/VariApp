using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace InventoryApp.Application.DTOs;

/// <summary>
/// Variante capturada dentro del formulario principal de productos.
/// El stock consolidado del producto se calcula como la suma de estas filas.
/// </summary>
public class ProductoVarianteFormularioDto
{
    public int? Id { get; set; }
    public int ColorId { get; set; }
    public string? Sku { get; set; }
    public string? CodigoBarras { get; set; }
    public int Cantidad { get; set; }
    public int UmbralStockBajo { get; set; } = 5;
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public bool Activo { get; set; } = true;
}

public class CreateProductoDto
{
    public string Nombre { get; set; } = string.Empty;

    // Compatibilidad temporal con clientes anteriores. Cuando se envían los IDs,
    // el backend obtiene Marca/Modelo desde sus mantenimientos.
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
    public TipoInventario TipoInventario { get; set; } = TipoInventario.MercaderiaVenta;
    public int Cantidad { get; set; }
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public int UmbralStockBajo { get; set; } = 5;
    public int? CategoriaId { get; set; }
    public int? ColorId { get; set; }
    public int? TallaId { get; set; }
    public int? MarcaId { get; set; }
    public int? ModeloId { get; set; }

    /// <summary>
    /// Colores/SKU del producto. El formulario nuevo siempre envía al menos uno.
    /// Se conserva compatibilidad con clientes anteriores que todavía no lo envían.
    /// </summary>
    public List<ProductoVarianteFormularioDto> Variantes { get; set; } = new();

    /// Máximo 5 imágenes. La primera se marca como principal.
    public List<IFormFile>? Imagenes { get; set; }
}
