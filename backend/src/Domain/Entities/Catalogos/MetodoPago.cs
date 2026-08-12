using InventoryApp.Domain.Common;

namespace InventoryApp.Domain.Entities.Catalogos;

/// <summary>
/// Catálogo relacional y administrable de métodos de pago.
/// Convive temporalmente con Domain.Enums.MetodoPago durante la migración ERP-N0.
/// </summary>
public class MetodoPago : AuditableEntity
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public bool RequiereReferencia { get; set; }
    public bool RequiereBanco { get; set; }
    public bool PermiteCambio { get; set; }
    public int Orden { get; set; }
    public string? Metadata { get; set; }

    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    /// <summary>
    /// Columna calculada usada para garantizar unicidad estable de Codigo
    /// sin depender de mayúsculas, minúsculas o espacios periféricos.
    /// </summary>
    public string? CodigoNormalizado { get; private set; }
}
