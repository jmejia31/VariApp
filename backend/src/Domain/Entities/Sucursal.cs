using InventoryApp.Domain.Common;

namespace InventoryApp.Domain.Entities;

/// <summary>
/// Sucursal operativa de una Empresa. N6.2 establece a Sucursal como la raíz
/// de ownership tenant-aware para la jerarquía Sucursal -> Almacen -> Ubicacion.
/// La relación física/FK y el backfill pertenecen al parent de persistencia.
/// </summary>
public class Sucursal : AuditableEntity
{
    /// <summary>
    /// Identificador de la Empresa propietaria. Permanece nullable temporalmente
    /// por compatibilidad con la persistencia previa hasta que N6.2.C complete
    /// backfill y constraints; el contrato de dominio tenant-aware exige un valor
    /// positivo cuando se resuelve ownership.
    /// </summary>
    public int? EmpresaId { get; set; }

    /// <summary>
    /// Resuelve de forma fail-closed la Empresa propietaria de la Sucursal.
    /// </summary>
    public int ObtenerEmpresaIdTenant()
    {
        if (!EmpresaId.HasValue || EmpresaId.Value <= 0)
        {
            throw new InvalidOperationException("La sucursal no tiene una Empresa tenant válida asignada.");
        }

        return EmpresaId.Value;
    }

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