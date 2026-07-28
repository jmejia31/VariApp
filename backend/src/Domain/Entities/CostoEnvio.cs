using InventoryApp.Domain.Common;

namespace InventoryApp.Domain.Entities;

public class CostoEnvio : AuditableEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Monto { get; set; }
    public DateTime? VigenteDesde { get; set; }
    public DateTime? VigenteHasta { get; set; }
    public int Prioridad { get; set; }
    public bool EsPredeterminado { get; set; }
    public bool Activo { get; set; } = true;
    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public bool EstaVigente(DateTime fechaUtc) =>
        Activo &&
        !Eliminado &&
        (!VigenteDesde.HasValue || VigenteDesde.Value <= fechaUtc) &&
        (!VigenteHasta.HasValue || VigenteHasta.Value >= fechaUtc);
}
