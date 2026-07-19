namespace InventoryApp.Application.Interfaces;

public class AdjuntoCorreo
{
    public string NombreArchivo { get; set; } = string.Empty;
    public byte[] Contenido { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}

/// Envío real de correo vía SMTP configurable (sección 15 del prompt).
/// Nunca coloca credenciales en el código: se leen de configuración/
/// variables de entorno (ver appsettings.json -> sección "Smtp", con
/// placeholders "CHANGE_ME" que deben sobrescribirse vía variables de
/// entorno en producción, nunca commiteados con valores reales).
public interface IEmailService
{
    Task<(bool Exito, string? Error)> EnviarAsync(string destinatario, string asunto, string cuerpoHtml, List<AdjuntoCorreo>? adjuntos = null);
}
