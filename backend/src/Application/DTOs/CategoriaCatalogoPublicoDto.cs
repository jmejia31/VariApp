namespace InventoryApp.Application.DTOs;

/// <summary>
/// Contrato minimo y seguro de categoria para navegacion publica.
/// </summary>
public sealed class CategoriaCatalogoPublicoDto
{
    public int Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public int TotalProductos { get; init; }
}
