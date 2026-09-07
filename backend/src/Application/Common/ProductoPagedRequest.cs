namespace InventoryApp.Application.Common;

/// <summary>
/// Filtros específicos del inventario. Todos son opcionales y se combinan con
/// búsqueda, ordenamiento y paginación sin alterar el contrato común.
/// </summary>
public sealed class ProductoPagedRequest : PagedRequest
{
    public int? CategoriaId { get; set; }
    public int? ColorId { get; set; }
    public int? TallaId { get; set; }
    public int? MarcaId { get; set; }
    public int? ModeloId { get; set; }
    public bool? Activo { get; set; }
    public bool? Agotado { get; set; }
}
