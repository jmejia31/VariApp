using InventoryApp.Domain.Enums;

namespace InventoryApp.Domain.Entities;

/// Bitácora centralizada de acciones del sistema. Complementa (no reemplaza) los
/// campos de auditoría que ya vive en cada entidad (CreadoPor/ConfirmadoPor/etc.) —
/// esta tabla permite ver TODO en una sola vista cronológica, filtrable.
public class RegistroAuditoria
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public int? UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;

    public ModuloSistema Modulo { get; set; }
    public AccionPermiso Accion { get; set; }

    /// Id del registro afectado (ProductoId, CompraId, VentaId, etc.), si aplica.
    public int? ReferenciaId { get; set; }

    /// Descripción legible: "Anuló la compra COM-000004 (motivo: producto dañado)".
    public string Descripcion { get; set; } = string.Empty;
}
