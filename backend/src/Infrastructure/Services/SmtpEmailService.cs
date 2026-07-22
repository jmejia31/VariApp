using System.Net;
using System.Net.Mail;
using InventoryApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InventoryApp.Infrastructure.Services;

/// Envío SMTP transaccional para facturas. Mantiene los secretos fuera del
/// código y normaliza las contraseñas de aplicación de Gmail, que suelen
/// copiarse visualmente en grupos separados por espacios.
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(bool Exito, string? Error)> EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        List<AdjuntoCorreo>? adjuntos = null)
    {
        var host = _configuration["Smtp:Host"]?.Trim();
        var puerto = _configuration.GetValue<int?>("Smtp:Port") ?? 587;
        var usuario = _configuration["Smtp:UsuarioSmtp"]?.Trim();
        var password = NormalizarPassword(_configuration["Smtp:PasswordSmtp"]);
        var usarSsl = _configuration.GetValue<bool?>("Smtp:UsarSsl") ?? true;
        var correoRemitente = _configuration["Smtp:CorreoRemitente"]?.Trim();
        var nombreRemitente = _configuration["Smtp:NombreRemitente"]?.Trim() ?? "VariStorehn Administrativo";

        if (string.IsNullOrWhiteSpace(host) || host == "CHANGE_ME" ||
            string.IsNullOrWhiteSpace(usuario) || usuario == "CHANGE_ME" ||
            string.IsNullOrWhiteSpace(password) || password == "CHANGE_ME")
        {
            _logger.LogWarning("SMTP no configurado: faltan Host, UsuarioSmtp o PasswordSmtp.");
            return (false, "El envío de correo no está configurado completamente.");
        }

        if (!MailAddress.TryCreate(destinatario?.Trim(), out var direccionDestino))
            return (false, "La dirección de correo del destinatario no es válida.");

        var remitenteTexto = string.IsNullOrWhiteSpace(correoRemitente) ? usuario : correoRemitente;
        if (!MailAddress.TryCreate(remitenteTexto, out var direccionRemitente))
            return (false, "El correo remitente configurado no es válido.");

        try
        {
            using var mensaje = new MailMessage
            {
                From = new MailAddress(direccionRemitente.Address, nombreRemitente),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8,
                HeadersEncoding = System.Text.Encoding.UTF8
            };
            mensaje.To.Add(direccionDestino);
            mensaje.ReplyToList.Add(new MailAddress(direccionRemitente.Address, nombreRemitente));

            if (adjuntos is not null)
            {
                foreach (var adjunto in adjuntos)
                {
                    if (adjunto.Contenido.Length == 0) continue;
                    var stream = new MemoryStream(adjunto.Contenido, writable: false);
                    mensaje.Attachments.Add(new Attachment(stream, adjunto.NombreArchivo, adjunto.ContentType));
                }
            }

            using var cliente = new SmtpClient(host, puerto)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(usuario, password),
                EnableSsl = usarSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000
            };

            await cliente.SendMailAsync(mensaje);
            _logger.LogInformation("Factura enviada por SMTP a {Destinatario} mediante {Host}:{Puerto}.", direccionDestino.Address, host, puerto);
            return (true, null);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex,
                "Fallo SMTP al enviar a {Destinatario}. StatusCode={StatusCode}, Host={Host}, Puerto={Puerto}",
                direccionDestino.Address, ex.StatusCode, host, puerto);

            var mensajeSeguro = ex.StatusCode == SmtpStatusCode.ClientNotPermitted ||
                                ex.StatusCode == SmtpStatusCode.MustIssueStartTlsFirst
                ? "El servidor SMTP rechazó la autenticación o la conexión segura. Revisa la contraseña de aplicación y TLS."
                : "El servidor de correo rechazó el envío. Revisa los registros del backend para conocer el motivo técnico.";

            return (false, mensajeSeguro);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo inesperado al enviar correo a {Destinatario}", direccionDestino.Address);
            return (false, "No se pudo enviar el correo. Intenta nuevamente y revisa los registros del backend.");
        }
    }

    private static string? NormalizarPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password)) return password;
        return new string(password.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }
}
