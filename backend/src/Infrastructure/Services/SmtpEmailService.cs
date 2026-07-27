using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using InventoryApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InventoryApp.Infrastructure.Services;

/// Envío SMTP transaccional para facturas. Mantiene los secretos fuera del
/// código, utiliza validación TLS del sistema operativo y aplica reintentos
/// acotados únicamente ante errores transitorios.
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

    public EstadoConfiguracionSmtp ObtenerEstadoConfiguracion()
    {
        var configuracion = LeerConfiguracion();
        var validacion = ValidarConfiguracion(configuracion);

        return new EstadoConfiguracionSmtp
        {
            Configurado = validacion.Error is null,
            Host = EnmascararHost(configuracion.Host),
            Puerto = configuracion.Puerto,
            UsaTls = configuracion.UsarSsl,
            RequiereAutenticacion = configuracion.RequiereAutenticacion,
            RemitenteEnmascarado = EnmascararCorreo(configuracion.CorreoRemitente),
            MaximoIntentos = configuracion.MaximoIntentos,
            TimeoutSegundos = configuracion.TimeoutSegundos,
            Mensaje = validacion.Error ?? "SMTP configurado. Los certificados TLS se validan con el almacén de confianza del sistema operativo."
        };
    }

    public async Task<ResultadoEntregaEmail> EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        List<AdjuntoCorreo>? adjuntos = null,
        CancellationToken cancellationToken = default)
    {
        var configuracion = LeerConfiguracion();
        var validacion = ValidarConfiguracion(configuracion);
        if (validacion.Error is not null)
        {
            _logger.LogWarning("SMTP no disponible: {Motivo}", validacion.Error);
            return Fallo("SMTP_NO_CONFIGURADO", validacion.Error, false, 0);
        }

        if (!MailAddress.TryCreate(destinatario?.Trim(), out var direccionDestino))
            return Fallo("DESTINATARIO_INVALIDO", "La dirección de correo del destinatario no es válida.", false, 0);

        asunto = LimpiarEncabezado(asunto);
        if (string.IsNullOrWhiteSpace(asunto))
            return Fallo("ASUNTO_INVALIDO", "El asunto del correo es obligatorio.", false, 0);

        cuerpoHtml ??= string.Empty;
        adjuntos ??= new List<AdjuntoCorreo>();
        var errorAdjuntos = ValidarAdjuntos(adjuntos);
        if (errorAdjuntos is not null)
            return Fallo("ADJUNTOS_INVALIDOS", errorAdjuntos, false, 0);

        var messageId = $"variapp-{Guid.NewGuid():N}";
        Exception? ultimaExcepcion = null;

        for (var intento = 1; intento <= configuracion.MaximoIntentos; intento++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var mensaje = CrearMensaje(
                    configuracion,
                    direccionDestino,
                    asunto,
                    cuerpoHtml,
                    adjuntos,
                    messageId);

                using var cliente = CrearCliente(configuracion);
                _logger.LogInformation(
                    "Intento SMTP {Intento}/{MaximoIntentos} para {DestinatarioEnmascarado} mediante {Host}:{Puerto}; TLS={Tls}; MessageId={MessageId}.",
                    intento,
                    configuracion.MaximoIntentos,
                    EnmascararCorreo(direccionDestino.Address),
                    configuracion.Host,
                    configuracion.Puerto,
                    configuracion.UsarSsl,
                    messageId);

                await cliente
                    .SendMailAsync(mensaje, cancellationToken)
                    .WaitAsync(TimeSpan.FromSeconds(configuracion.TimeoutSegundos), cancellationToken);

                _logger.LogInformation(
                    "Correo enviado a {DestinatarioEnmascarado} en {Intentos} intento(s). MessageId={MessageId}.",
                    EnmascararCorreo(direccionDestino.Address),
                    intento,
                    messageId);

                return new ResultadoEntregaEmail
                {
                    Exito = true,
                    Codigo = "ENVIADO",
                    Intentos = intento,
                    MessageId = messageId
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Envío SMTP cancelado para {DestinatarioEnmascarado}. MessageId={MessageId}.",
                    EnmascararCorreo(direccionDestino.Address),
                    messageId);
                throw;
            }
            catch (Exception ex) when (EsErrorTransitorio(ex) && intento < configuracion.MaximoIntentos)
            {
                ultimaExcepcion = ex;
                var demora = CalcularDemora(configuracion.RetryBaseDelayMilliseconds, intento);
                _logger.LogWarning(
                    ex,
                    "Fallo SMTP transitorio en intento {Intento}/{MaximoIntentos}; se reintentará en {DemoraMs} ms. Destinatario={DestinatarioEnmascarado}; MessageId={MessageId}.",
                    intento,
                    configuracion.MaximoIntentos,
                    demora.TotalMilliseconds,
                    EnmascararCorreo(direccionDestino.Address),
                    messageId);
                await Task.Delay(demora, cancellationToken);
            }
            catch (Exception ex)
            {
                ultimaExcepcion = ex;
                return CrearFalloTecnico(ex, direccionDestino.Address, configuracion, intento, messageId);
            }
        }

        return CrearFalloTecnico(
            ultimaExcepcion ?? new TimeoutException("El servidor SMTP no respondió."),
            direccionDestino.Address,
            configuracion,
            configuracion.MaximoIntentos,
            messageId);
    }

    private ConfiguracionSmtp LeerConfiguracion()
    {
        var usuario = _configuration["Smtp:UsuarioSmtp"]?.Trim() ?? string.Empty;
        var remitente = _configuration["Smtp:CorreoRemitente"]?.Trim();
        if (string.IsNullOrWhiteSpace(remitente)) remitente = usuario;

        return new ConfiguracionSmtp(
            Host: _configuration["Smtp:Host"]?.Trim() ?? string.Empty,
            Puerto: _configuration.GetValue<int?>("Smtp:Port") ?? 587,
            Usuario: usuario,
            Password: NormalizarPassword(_configuration["Smtp:PasswordSmtp"]) ?? string.Empty,
            UsarSsl: _configuration.GetValue<bool?>("Smtp:UsarSsl") ?? true,
            RequiereAutenticacion: _configuration.GetValue<bool?>("Smtp:RequiereAutenticacion") ?? true,
            CorreoRemitente: remitente ?? string.Empty,
            NombreRemitente: LimpiarEncabezado(_configuration["Smtp:NombreRemitente"] ?? "VariStorehn"),
            CorreoRespuesta: _configuration["Smtp:CorreoRespuesta"]?.Trim(),
            TimeoutSegundos: Math.Clamp(_configuration.GetValue<int?>("Smtp:TimeoutSeconds") ?? 30, 5, 120),
            MaximoIntentos: Math.Clamp(_configuration.GetValue<int?>("Smtp:MaxAttempts") ?? 3, 1, 5),
            RetryBaseDelayMilliseconds: Math.Clamp(_configuration.GetValue<int?>("Smtp:RetryBaseDelayMilliseconds") ?? 500, 50, 10_000));
    }

    private static (string? Error, MailAddress? Remitente) ValidarConfiguracion(ConfiguracionSmtp configuracion)
    {
        if (string.IsNullOrWhiteSpace(configuracion.Host) || EsPlaceholder(configuracion.Host))
            return ("El host SMTP de Desarrollo no está configurado.", null);

        if (configuracion.Puerto is < 1 or > 65535)
            return ("El puerto SMTP configurado no es válido.", null);

        if (configuracion.RequiereAutenticacion &&
            (string.IsNullOrWhiteSpace(configuracion.Usuario) || EsPlaceholder(configuracion.Usuario) ||
             string.IsNullOrWhiteSpace(configuracion.Password) || EsPlaceholder(configuracion.Password)))
        {
            return ("Las credenciales SMTP de Desarrollo no están configuradas completamente.", null);
        }

        if (!MailAddress.TryCreate(configuracion.CorreoRemitente, out var remitente))
            return ("El correo remitente SMTP configurado no es válido.", null);

        if (!string.IsNullOrWhiteSpace(configuracion.CorreoRespuesta) &&
            !MailAddress.TryCreate(configuracion.CorreoRespuesta, out _))
        {
            return ("El correo de respuesta SMTP configurado no es válido.", null);
        }

        return (null, remitente);
    }

    private static string? ValidarAdjuntos(List<AdjuntoCorreo> adjuntos)
    {
        if (adjuntos.Count > MaximoAdjuntos)
            return $"El correo no puede incluir más de {MaximoAdjuntos} archivos adjuntos.";

        if (adjuntos.Any(a => a.Contenido is not { Length: > 0 }))
            return "Todos los archivos adjuntos deben contener datos.";

        var totalAdjuntos = adjuntos.Sum(a => (long)a.Contenido.Length);
        if (totalAdjuntos > MaximoTotalAdjuntosBytes)
            return "Los archivos adjuntos superan el límite total permitido de 20 MB.";

        return null;
    }

    private static MailMessage CrearMensaje(
        ConfiguracionSmtp configuracion,
        MailAddress destino,
        string asunto,
        string cuerpoHtml,
        List<AdjuntoCorreo> adjuntos,
        string messageId)
    {
        var validacion = ValidarConfiguracion(configuracion);
        var remitente = validacion.Remitente
            ?? throw new InvalidOperationException(validacion.Error ?? "Configuración SMTP inválida.");

        var mensaje = new MailMessage
        {
            From = new MailAddress(remitente.Address, configuracion.NombreRemitente, Encoding.UTF8),
            Subject = asunto,
            Body = cuerpoHtml,
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
            HeadersEncoding = Encoding.UTF8
        };

        mensaje.Headers["X-VariApp-Message-Id"] = messageId;
        mensaje.Headers["X-Auto-Response-Suppress"] = "All";
        mensaje.To.Add(destino);

        var respuesta = string.IsNullOrWhiteSpace(configuracion.CorreoRespuesta)
            ? remitente.Address
            : configuracion.CorreoRespuesta;
        mensaje.ReplyToList.Add(new MailAddress(respuesta!, configuracion.NombreRemitente, Encoding.UTF8));

        var cuerpoTexto = ConvertirHtmlATexto(cuerpoHtml);
        mensaje.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            cuerpoTexto,
            Encoding.UTF8,
            MediaTypeNames.Text.Plain));
        mensaje.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            cuerpoHtml,
            Encoding.UTF8,
            MediaTypeNames.Text.Html));

        foreach (var adjunto in adjuntos)
        {
            var nombreArchivo = LimpiarNombreArchivo(adjunto.NombreArchivo);
            var contentType = string.IsNullOrWhiteSpace(adjunto.ContentType)
                ? MediaTypeNames.Application.Octet
                : adjunto.ContentType.Trim();
            var stream = new MemoryStream(adjunto.Contenido, writable: false);
            mensaje.Attachments.Add(new Attachment(stream, nombreArchivo, contentType));
        }

        return mensaje;
    }

    private static SmtpClient CrearCliente(ConfiguracionSmtp configuracion)
    {
        var cliente = new SmtpClient(configuracion.Host, configuracion.Puerto)
        {
            UseDefaultCredentials = false,
            EnableSsl = configuracion.UsarSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = configuracion.TimeoutSegundos * 1000
        };

        cliente.Credentials = configuracion.RequiereAutenticacion
            ? new NetworkCredential(configuracion.Usuario, configuracion.Password)
            : CredentialCache.DefaultNetworkCredentials;

        return cliente;
    }

    private ResultadoEntregaEmail CrearFalloTecnico(
        Exception ex,
        string destinatario,
        ConfiguracionSmtp configuracion,
        int intentos,
        string messageId)
    {
        var transitorio = EsErrorTransitorio(ex);
        var (codigo, mensaje) = MapearError(ex, transitorio);

        _logger.LogError(
            ex,
            "Fallo SMTP definitivo. Codigo={Codigo}; Intentos={Intentos}; Destinatario={DestinatarioEnmascarado}; Host={Host}; Puerto={Puerto}; TLS={Tls}; MessageId={MessageId}.",
            codigo,
            intentos,
            EnmascararCorreo(destinatario),
            configuracion.Host,
            configuracion.Puerto,
            configuracion.UsarSsl,
            messageId);

        return Fallo(codigo, mensaje, transitorio, intentos, messageId);
    }

    private static (string Codigo, string Mensaje) MapearError(Exception ex, bool transitorio)
    {
        if (ex is TimeoutException)
            return ("SMTP_TIMEOUT", "El servidor de correo no respondió dentro del tiempo permitido. Intenta nuevamente.");

        if (ex is SmtpException smtp)
        {
            if (smtp.StatusCode is SmtpStatusCode.ClientNotPermitted or SmtpStatusCode.MustIssueStartTlsFirst)
            {
                return ("SMTP_AUTENTICACION", "El servidor SMTP rechazó la autenticación o la conexión segura. Revisa la contraseña de aplicación y TLS en Desarrollo.");
            }

            if (smtp.StatusCode is SmtpStatusCode.MailboxUnavailable)
                return ("SMTP_DESTINATARIO_RECHAZADO", "El servidor rechazó el buzón del destinatario. Verifica la dirección de correo.");

            if (transitorio)
                return ("SMTP_TEMPORAL", "El servidor de correo presentó un problema temporal después de varios intentos. Intenta nuevamente más tarde.");

            return ("SMTP_RECHAZADO", "El servidor de correo rechazó el envío. Revisa los registros del backend de Desarrollo.");
        }

        if (transitorio)
            return ("SMTP_TEMPORAL", "No fue posible establecer una conexión estable con el servidor de correo. Intenta nuevamente.");

        return ("SMTP_ERROR", "No se pudo enviar el correo. Revisa los registros del backend de Desarrollo.");
    }

    private static bool EsErrorTransitorio(Exception ex)
    {
        if (ex is TimeoutException or IOException or SocketException)
            return true;

        if (ex.InnerException is IOException or SocketException or TimeoutException)
            return true;

        return ex is SmtpException smtp && smtp.StatusCode is
            SmtpStatusCode.ServiceNotAvailable or
            SmtpStatusCode.MailboxBusy or
            SmtpStatusCode.InsufficientStorage or
            SmtpStatusCode.LocalErrorInProcessing or
            SmtpStatusCode.TransactionFailed or
            SmtpStatusCode.GeneralFailure;
    }

    private static TimeSpan CalcularDemora(int baseMilliseconds, int intento)
    {
        var factor = Math.Pow(2, Math.Max(0, intento - 1));
        return TimeSpan.FromMilliseconds(Math.Min(baseMilliseconds * factor, 10_000));
    }

    private static ResultadoEntregaEmail Fallo(
        string codigo,
        string error,
        bool esTransitorio,
        int intentos,
        string? messageId = null) => new()
        {
            Exito = false,
            Codigo = codigo,
            Error = error,
            EsTransitorio = esTransitorio,
            Intentos = intentos,
            MessageId = messageId
        };

    private static bool EsPlaceholder(string valor) =>
        valor.Equals("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
        valor.Equals("REPLACE_ME", StringComparison.OrdinalIgnoreCase) ||
        valor.Contains("not-used", StringComparison.OrdinalIgnoreCase);

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

    private static string ConvertirHtmlATexto(string html)
    {
        var texto = Regex.Replace(html, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
        texto = Regex.Replace(texto, "</p>", "\n\n", RegexOptions.IgnoreCase);
        texto = Regex.Replace(texto, "<[^>]+>", string.Empty);
        texto = WebUtility.HtmlDecode(texto);
        return Regex.Replace(texto, "[ \\t]+", " ").Trim();
    }

    private static string EnmascararCorreo(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo)) return "***";
        var partes = correo.Split('@', 2);
        if (partes.Length != 2 || partes[0].Length == 0) return "***";
        var usuario = partes[0].Length <= 2 ? partes[0][0] + "***" : partes[0][..2] + "***";
        return $"{usuario}@{partes[1]}";
    }

    private static string EnmascararHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host) || EsPlaceholder(host)) return "No configurado";
        var partes = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length <= 2 ? host : $"***.{string.Join('.', partes[^2..])}";
    }

    private sealed record ConfiguracionSmtp(
        string Host,
        int Puerto,
        string Usuario,
        string Password,
        bool UsarSsl,
        bool RequiereAutenticacion,
        string CorreoRemitente,
        string NombreRemitente,
        string? CorreoRespuesta,
        int TimeoutSegundos,
        int MaximoIntentos,
        int RetryBaseDelayMilliseconds);
}
