namespace InventoryApp.Application.DTOs;

public class RegistroAuditoriaDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public int? ReferenciaId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

public class AuditoriaFiltroDto
{
    public int? UsuarioId { get; set; }
    public string? Modulo { get; set; }
    public string? Accion { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
