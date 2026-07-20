using System.Net;
using System.Net.Mail;
using InventoryApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InventoryApp.Infrastructure.Services;

/// Usa System.Net.Mail.SmtpClient (parte del framework base de .NET, sin
/// dependencias NuGet nuevas) en vez de MailKit. Decisión deliberada: dado
/// que QuestPDF (fase 4) ya quedó sin poder verificarse compilando, se
/// evita sumar una segunda dependencia externa sin verificar en la misma
/// sesión. SmtpClient está marcado obsoleto para escenarios avanzados
/// (OAuth2, etc.) pero sigue siendo válido y soportado para SMTP básico
/// con usuario/contraseña, que es el caso de uso aquí.
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(bool Exito, string? Error)> EnviarAsync(string destinatario, string asunto, string cuerpoHtml, List<AdjuntoCorreo>? adjuntos = null)
    {
        var host = _configuration["Smtp:Host"];
        var puerto = _configuration.GetValue<int?>("Smtp:Port") ?? 587;
        var usuario = _configuration["Smtp:UsuarioSmtp"];
        var password = _configuration["Smtp:PasswordSmtp"];
        var usarSsl = _configuration.GetValue<bool?>("Smtp:UsarSsl") ?? true;
        var correoRemitente = _configuration["Smtp:CorreoRemitente"];
        var nombreRemitente = _configuration["Smtp:NombreRemitente"] ?? "VariStorehn";

        // No se intenta conectar con placeholders sin configurar: error
        // claro en vez de una excepción de red confusa (sección 18:
        // "manejar errores" / sección 12: "define qué ocurre si falla").
        if (string.IsNullOrWhiteSpace(host) || host == "CHANGE_ME" ||
            string.IsNullOrWhiteSpace(usuario) || usuario == "CHANGE_ME" ||
            string.IsNullOrWhiteSpace(password) || password == "CHANGE_ME")
        {
            return (false, "El envío de correo no está configurado todavía. Contacta al administrador del sistema.");
        }

        try
        {
            using var mensaje = new MailMessage
            {
                From = new MailAddress(correoRemitente ?? usuario, nombreRemitente),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true
            };
            mensaje.To.Add(destinatario);

            if (adjuntos is not null)
            {
                foreach (var adjunto in adjuntos)
                {
                    var stream = new MemoryStream(adjunto.Contenido);
                    mensaje.Attachments.Add(new Attachment(stream, adjunto.NombreArchivo, adjunto.ContentType));
                }
            }

            using var cliente = new SmtpClient(host, puerto)
            {
                Credentials = new NetworkCredential(usuario, password),
                EnableSsl = usarSsl
            };

            await cliente.SendMailAsync(mensaje);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo al enviar correo a {Destinatario}", destinatario);
            // Mensaje genérico al usuario final (sección 17: "no expongas...
            // trazas completas al usuario final"); el detalle real queda en logs.
            return (false, "No se pudo enviar el correo. Verifica la dirección o intenta más tarde.");
        }
    }
}
