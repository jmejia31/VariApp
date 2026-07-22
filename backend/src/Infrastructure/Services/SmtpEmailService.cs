using System.Net;
using System.Net.Mail;
using System.Text;
using InventoryApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InventoryApp.Infrastructure.Services;

/// Envío SMTP transaccional para facturas. Mantiene los secretos fuera del
/// código, normaliza contraseñas de aplicación y devuelve mensajes seguros.
public class SmtpEmailService : IEmailService
{
    private const int MaximoAdjuntos = 5;
    private const int MaximoTotalAdjuntosBytes = 20 * 1024 * 1024;

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
        var nombreRemitente = _configuration["Smtp:NombreRemitente"]?.Trim() ?? "VariStorehn";
        var timeoutSegundos = Math.Clamp(_configuration.GetValue<int?>("Smtp:TimeoutSeconds") ?? 30, 10, 120);

        if (string.IsNullOrWhiteSpace(host) || EsPlaceholder(host) ||
            string.IsNullOrWhiteSpace(usuario) || EsPlaceholder(usuario) ||
            string.IsNullOrWhiteSpace(password) || EsPlaceholder(password))
        {
            _logger.LogWarning("SMTP no configurado: faltan Host, UsuarioSmtp o PasswordSmtp.");
            return (false, "El envío de correo no está configurado completamente.");
        }

        if (puerto is < 1 or > 65535)
            return (false, "El puerto SMTP configurado no es válido.");

        if (!MailAddress.TryCreate(destinatario?.Trim(), out var direccionDestino))
            return (false, "La dirección de correo del destinatario no es válida.");

        if (!MailAddress.TryCreate(usuario, out var direccionUsuario))
            return (false, "El usuario SMTP configurado no es una dirección válida.");

        var remitenteTexto = string.IsNullOrWhiteSpace(correoRemitente) ? direccionUsuario.Address : correoRemitente;
        if (!MailAddress.TryCreate(remitenteTexto, out var direccionRemitente))
            return (false, "El correo remitente configurado no es válido.");

        asunto = LimpiarEncabezado(asunto);
        if (string.IsNullOrWhiteSpace(asunto))
            return (false, "El asunto del correo es obligatorio.");

        cuerpoHtml ??= string.Empty;
        adjuntos ??= new List<AdjuntoCorreo>();
        if (adjuntos.Count > MaximoAdjuntos)
            return (false, $"El correo no puede incluir más de {MaximoAdjuntos} archivos adjuntos.");

        var totalAdjuntos = adjuntos.Sum(a => a.Contenido?.Length ?? 0);
        if (totalAdjuntos > MaximoTotalAdjuntosBytes)
            return (false, "Los archivos adjuntos superan el límite total permitido de 20 MB.");

        try
        {
            using var mensaje = new MailMessage
            {
                From = new MailAddress(direccionRemitente.Address, nombreRemitente, Encoding.UTF8),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8,
                HeadersEncoding = Encoding.UTF8
            };
            mensaje.To.Add(direccionDestino);
            mensaje.ReplyToList.Add(new MailAddress(direccionRemitente.Address, nombreRemitente, Encoding.UTF8));

            foreach (var adjunto in adjuntos)
            {
                if (adjunto.Contenido is not { Length: > 0 }) continue;

                var nombreArchivo = LimpiarNombreArchivo(adjunto.NombreArchivo);
                var contentType = string.IsNullOrWhiteSpace(adjunto.ContentType)
                    ? "application/octet-stream"
                    : adjunto.ContentType.Trim();
                var stream = new MemoryStream(adjunto.Contenido, writable: false);
                mensaje.Attachments.Add(new Attachment(stream, nombreArchivo, contentType));
            }

            using var cliente = new SmtpClient(host, puerto)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(direccionUsuario.Address, password),
                EnableSsl = usarSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = timeoutSegundos * 1000
            };

            await cliente.SendMailAsync(mensaje);
            _logger.LogInformation(
                "Factura enviada por SMTP a {DestinatarioEnmascarado} mediante {Host}:{Puerto}.",
                EnmascararCorreo(direccionDestino.Address),
                host,
                puerto);
            return (true, null);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(
                ex,
                "Fallo SMTP al enviar a {DestinatarioEnmascarado}. StatusCode={StatusCode}, Host={Host}, Puerto={Puerto}",
                EnmascararCorreo(direccionDestino.Address),
                ex.StatusCode,
                host,
                puerto);

            var mensajeSeguro = ex.StatusCode switch
            {
                SmtpStatusCode.ClientNotPermitted or SmtpStatusCode.MustIssueStartTlsFirst =>
                    "El servidor SMTP rechazó la autenticación o la conexión segura. Revisa la contraseña de aplicación y TLS.",
                SmtpStatusCode.MailboxUnavailable or SmtpStatusCode.MailboxBusy =>
                    "El servidor rechazó temporalmente el buzón del destinatario. Verifica la dirección o intenta más tarde.",
                _ => "El servidor de correo rechazó el envío. Revisa los registros del backend para conocer el motivo técnico."
            };

            return (false, mensajeSeguro);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Fallo inesperado al enviar correo a {DestinatarioEnmascarado}.",
                EnmascararCorreo(direccionDestino.Address));
            return (false, "No se pudo enviar el correo. Intenta nuevamente y revisa los registros del backend.");
        }
    }

    private static bool EsPlaceholder(string valor) =>
        valor.Equals("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
        valor.Equals("REPLACE_ME", StringComparison.OrdinalIgnoreCase);

    private static string? NormalizarPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password)) return password;
        return new string(password.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    private static string LimpiarEncabezado(string? valor) =>
        (valor ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();

    private static string LimpiarNombreArchivo(string? nombre)
    {
        var limpio = Path.GetFileName(nombre ?? "adjunto.bin");
        return string.IsNullOrWhiteSpace(limpio) ? "adjunto.bin" : limpio;
    }

    private static string EnmascararCorreo(string correo)
    {
        var partes = correo.Split('@', 2);
        if (partes.Length != 2 || partes[0].Length == 0) return "***";
        var usuario = partes[0].Length <= 2 ? partes[0][0] + "***" : partes[0][..2] + "***";
        return $"{usuario}@{partes[1]}";
    }
}