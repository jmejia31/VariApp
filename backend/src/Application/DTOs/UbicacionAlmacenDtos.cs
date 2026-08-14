namespace InventoryApp.Application.DTOs;

public sealed class UbicacionAlmacenDto
{
    public int Id { get; set; }
    public int AlmacenId { get; set; }
    public string AlmacenCodigo { get; set; } = string.Empty;
    public string AlmacenNombre { get; set; } = string.Empty;
    public int? UbicacionPadreId { get; set; }
    public string? UbicacionPadreCodigo { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public bool Activa { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}

public sealed class CreateUbicacionAlmacenDto
{
    public int AlmacenId { get; set; }
    public int? UbicacionPadreId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
}

public sealed class UpdateUbicacionAlmacenDto
{
    public int AlmacenId { get; set; }
    public int? UbicacionPadreId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
}

public sealed class UbicacionAlmacenFiltroDto
{
    public string? Buscar { get; set; }
    public int? AlmacenId { get; set; }
    public int? UbicacionPadreId { get; set; }
    public string? Tipo { get; set; }
    public bool? Activa { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;
}

public sealed class UbicacionAlmacenPaginaDto
{
    public List<UbicacionAlmacenDto> Items { get; set; } = new();
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int Total { get; set; }
    public int TotalPaginas { get; set; }
}

public sealed class TipoUbicacionAlmacenDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}
