namespace InventoryApp.Application.DTOs;

/// <summary>
/// Proyección segura para el escaparate público. Excluye costos, auditoría,
/// códigos internos y cualquier dato reservado a la administración.
/// </summary>
public sealed class ProductoCatalogoPublicoDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public string? CategoriaNombre { get; init; }
    public string? MarcaNombre { get; init; }
    public string? ModeloNombre { get; init; }
    public decimal Precio { get; init; }
    public int CantidadDisponible { get; init; }
    public bool EstaAgotado { get; init; }
    public string? ImagenPrincipalUrl { get; init; }
    public List<ProductoImagenPublicaDto> Imagenes { get; init; } = new();
    public List<ModeloCatalogoPublicoDto> Modelos { get; init; } = new();
}

public sealed class ProductoImagenPublicaDto
{
    public string Url { get; init; } = string.Empty;
    public int Orden { get; init; }
    public bool EsPrincipal { get; init; }
}

public sealed class ModeloCatalogoPublicoDto
{
    public int? ModeloId { get; init; }
    public string? ModeloNombre { get; init; }
    public string? MarcaNombre { get; init; }
    public decimal Precio { get; init; }
    public int CantidadDisponible { get; init; }
    public bool EstaAgotado { get; init; }
    public List<ProductoImagenPublicaDto> Imagenes { get; init; } = new();
}
