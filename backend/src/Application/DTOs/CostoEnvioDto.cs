namespace InventoryApp.Application.DTOs;

public class CostoEnvioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Monto { get; set; }
    public DateTime? VigenteDesde { get; set; }
    public DateTime? VigenteHasta { get; set; }
    public int Prioridad { get; set; }
    public bool EsPredeterminado { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public class GuardarCostoEnvioDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Monto { get; set; }
    public DateTime? VigenteDesde { get; set; }
    public DateTime? VigenteHasta { get; set; }
    public int Prioridad { get; set; }
    public bool EsPredeterminado { get; set; }
    public bool Activo { get; set; } = true;
}
