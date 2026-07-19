namespace InventoryApp.Domain.Entities;

/// Enlace público y temporal para acceder al PDF de una factura SIN el JWT
/// del usuario (necesario porque WhatsApp/el destinatario del correo no
/// tiene sesión en el sistema). Sección 14 del prompt: "obtener un enlace
/// accesible y autorizado" — autorizado no significa "requiere login", sino
/// "válido, con expiración, trazable a quién lo generó y para qué factura".
/// No sustituye la autenticación normal del sistema: es un mecanismo
/// deliberadamente distinto y más restringido (de un solo documento,
/// expira, queda auditado).
public class EnlacePublicoFactura
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public int FacturaId { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaExpiracion { get; set; }
    public int? CreadoPorUsuarioId { get; set; }

    public int VecesAccedido { get; set; }
    public DateTime? UltimoAcceso { get; set; }
}
