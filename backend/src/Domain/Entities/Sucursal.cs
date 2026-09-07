using InventoryApp.Domain.Common;

namespace InventoryApp.Domain.Entities;

/// <summary>
/// Sucursal operativa de la empresa. EmpresaId se reserva para la evolución
/// multiempresa de ERP-N6 y no constituye todavía una relación tenant-aware.
/// </summary>
public class Sucursal : AuditableEntity
{
    /// <summary>
    /// Identificador futuro de empresa/tenant. Permanece nullable hasta que ERP-N6
    /// introduzca la entidad raíz y su aislamiento; N1 no crea una FK ficticia.
    /// </summary>
    public int? EmpresaId { get; set; }

    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string ZonaHoraria { get; set; } = "America/Tegucigalpa";
    public bool Activa { get; set; } = true;

    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }
}
