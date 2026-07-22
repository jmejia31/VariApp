using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace InventoryApp.Application.Services;

public class FacturaCompartirService : IFacturaCompartirService
{
    private const int HorasValidezPredeterminadas = 72;
    private const int MaximoAccesosPredeterminado = 25;

    private readonly IFacturaCompartirRepository _repository;
    private readonly IFacturaService _facturaService;
    private readonly IFacturaPdfService _facturaPdfService;
    private readonly IEmailService _emailService;
    private readonly IAuditoriaService _auditoria;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FacturaCompartirService(
        IFacturaCompartirRepository repository,
        IFacturaService facturaService,
        IFacturaPdfService facturaPdfService,
        IEmailService emailService,
        IAuditoriaService auditoria,
        ICurrentUserService currentUser,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _facturaService = facturaService;
        _facturaPdfService = facturaPdfService;
        _emailService = emailService;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<EnlaceCompartirDto> PrepararCompartirAsync(int facturaId)
    {
        var factura = await ObtenerFacturaCompartibleAsync(facturaId);
        var backendUrl = ResolverBackendPublicUrl();
        var ahora = DateTime.UtcNow;
        var horasValidez = Math.Clamp(
            _configuration.GetValue<int?>("AppSettings:EnlacePublicoFacturaHorasValidez")
                ?? ((_configuration.GetValue<int?>("AppSettings:EnlacePublicoFacturaDiasValidez") ?? 3) * 24),
            1,
            168);

        // Un enlace nuevo invalida los anteriores. Como solo se persiste el hash,
        // el token secreto no puede recuperarse ni reutilizarse posteriormente.
        var enlacesRevocados = await _repository.ExpirarVigentesAsync(facturaId, ahora);
        var tokenPublico = GenerarTokenSeguro();
        var enlace = new EnlacePublicoFactura
        {
            Token = CalcularHashToken(tokenPublico),
            FacturaId = facturaId,
            FechaCreacion = ahora,
            FechaExpiracion = ahora.AddHours(horasValidez),
            CreadoPorUsuarioId = _currentUser.UsuarioId
        };

        await _repository.AddEnlaceAsync(enlace);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Facturacion,
            AccionPermiso.Compartir,
            $"Enlace temporal generado para la factura {factura.NumeroFactura}.",
            facturaId,
            entidad: "Factura",
            valoresNuevos: new
            {
                EnlaceId = enlace.Id,
                enlace.FechaExpiracion,
                EnlacesAnterioresRevocados = enlacesRevocados
            });

        var urlPublica = $"{backendUrl}/facturas/publico/{tokenPublico}/pdf";
        var mensaje =
            $"Estimado/a {factura.ClienteNombre}, le compartimos la factura correspondiente a su compra " +
            $"realizada en {factura.EmpresaNombre}. Número de factura: {factura.NumeroFactura}. " +
            $"Total: L. {factura.Total:N2}. Puede descargarla aquí: {urlPublica} " +
            $"El enlace estará disponible hasta el {enlace.FechaExpiracion.ToLocalTime():dd/MM/yyyy HH:mm}. " +
            "Gracias por su preferencia.";

        return new EnlaceCompartirDto
        {
            UrlPdfPublica = urlPublica,
            FechaExpiracion = enlace.FechaExpiracion,
            MensajeWhatsApp = mensaje,
            TelefonoSugerido = NormalizarTelefonoHonduras(factura.ClienteTelefono)
        };
    }

    public async Task RegistrarIntentoAsync(int facturaId, RegistrarEnvioDto dto)
    {
        var factura = await _facturaService.GetByIdAsync(facturaId)
            ?? throw new BusinessRuleException("La factura no existe o no está disponible para el usuario actual.");

        var canal = dto.Canal?.Trim();
        if (!string.Equals(canal, "WhatsApp", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(canal, "Correo", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("El canal de envío debe ser WhatsApp o Correo.");
        }

        var destinatario = Truncar(dto.Destinatario?.Trim(), 200);
        if (string.IsNullOrWhiteSpace(destinatario))
            throw new BusinessRuleException("El destinatario del envío es obligatorio.");

        var resultado = string.IsNullOrWhiteSpace(dto.Resultado) ? "Iniciado" : Truncar(dto.Resultado.Trim(), 50)!;
        var error = Truncar(dto.Error?.Trim(), 500);

        await _repository.AddHistorialAsync(new HistorialEnvioFactura
        {
            FacturaId = facturaId,
            Canal = string.Equals(canal, "Correo", StringComparison.OrdinalIgnoreCase) ? "Correo" : "WhatsApp",
            Destinatario = destinatario,
            Resultado = resultado,
            Error = error,
            UsuarioId = _currentUser.UsuarioId,
            UsuarioNombre = _currentUser.NombreUsuario
        });
        await _repository.SaveChangesAsync();

        var esError = string.Equals(resultado, "Error", StringComparison.OrdinalIgnoreCase);
        await _auditoria.RegistrarAsync(
            ModuloSistema.Facturacion,
            AccionPermiso.Compartir,
            $"Intento de envío de la factura {factura.NumeroFactura} por {canal}: {resultado}.",
            facturaId,
            entidad: "Factura",
            resultado: esError ? "Error" : "Exito",
            error: error,
            valoresNuevos: new { Canal = canal, Destinatario = destinatario, Resultado = resultado });
    }

    public async Task<List<HistorialEnvioDto>> GetHistorialAsync(int facturaId)
    {
        _ = await _facturaService.GetByIdAsync(facturaId)
            ?? throw new BusinessRuleException("La factura no existe o no está disponible para el usuario actual.");

        var historial = await _repository.GetHistorialAsync(facturaId);
        return historial.Select(h => new HistorialEnvioDto
        {
            Id = h.Id,
            Canal = h.Canal,
            Destinatario = h.Destinatario,
            Resultado = h.Resultado,
            Error = h.Error,
            UsuarioNombre = h.UsuarioNombre,
            Fecha = h.Fecha
        }).ToList();
    }

    public async Task<int> RevocarEnlacesAsync(int facturaId)
    {
        var factura = await _facturaService.GetByIdAsync(facturaId)
            ?? throw new BusinessRuleException("La factura no existe o no está disponible para el usuario actual.");

        var ahora = DateTime.UtcNow;
        var revocados = await _repository.ExpirarVigentesAsync(facturaId, ahora);
        if (revocados > 0)
            await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Facturacion,
            AccionPermiso.Compartir,
            $"Se revocaron {revocados} enlace(s) público(s) de la factura {factura.NumeroFactura}.",
            facturaId,
            entidad: "Factura",
            valoresNuevos: new { EnlacesRevocados = revocados, FechaRevocacion = ahora });

        return revocados;
    }

    public async Task<(byte[] Pdf, string NombreArchivo)?> ObtenerPdfPorTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 32 || token.Length > 160)
            return null;

        var enlace = await _repository.GetPorTokenHashAsync(CalcularHashToken(token));
        if (enlace is null || enlace.FechaExpiracion <= DateTime.UtcNow)
            return null;

        var maximoAccesos = Math.Clamp(
            _configuration.GetValue<int?>("AppSettings:EnlacePublicoFacturaMaximoAccesos")
                ?? MaximoAccesosPredeterminado,
            1,
            500);

        if (enlace.VecesAccedido >= maximoAccesos)
            return null;

        var factura = await _facturaService.GetByIdAsync(enlace.FacturaId);
        if (factura is null || string.Equals(factura.Estado, "Anulada", StringComparison.OrdinalIgnoreCase))
            return null;

        enlace.VecesAccedido += 1;
        enlace.UltimoAcceso = DateTime.UtcNow;
        _repository.UpdateEnlace(enlace);
        await _repository.SaveChangesAsync();

        var pdf = await _facturaPdfService.GenerarPdfAsync(factura);

        await _auditoria.RegistrarAsync(
            ModuloSistema.Facturacion,
            AccionPermiso.Exportar,
            $"PDF público descargado mediante enlace temporal para la factura {factura.NumeroFactura}.",
            enlace.Id,
            entidad: "EnlacePublicoFactura",
            valoresNuevos: new
            {
                enlace.FacturaId,
                enlace.VecesAccedido,
                enlace.UltimoAcceso
            });

        return (pdf, $"{factura.NumeroFactura}.pdf");
    }

    public async Task<(bool Exito, string Mensaje)> EnviarPorCorreoAsync(int facturaId, string destinatario)
    {
        destinatario = destinatario?.Trim() ?? string.Empty;
        if (!EsCorreoValido(destinatario))
            return (false, "El correo indicado no tiene un formato válido.");

        var factura = await ObtenerFacturaCompartibleAsync(facturaId);

        byte[] pdf;
        try
        {
            pdf = await _facturaPdfService.GenerarPdfAsync(factura);
        }
        catch
        {
            await RegistrarIntentoAsync(facturaId, new RegistrarEnvioDto
            {
                Canal = "Correo",
                Destinatario = destinatario,
                Resultado = "Error",
                Error = "No fue posible generar el PDF oficial de la factura."
            });
            return (false, "No se pudo generar el PDF de la factura para enviarlo por correo.");
        }

        var cliente = WebUtility.HtmlEncode(factura.ClienteNombre);
        var empresa = WebUtility.HtmlEncode(factura.EmpresaNombre);
        var numeroFactura = WebUtility.HtmlEncode(factura.NumeroFactura);
        var asunto = $"Factura {factura.NumeroFactura} - {factura.EmpresaNombre}";
        var cuerpo = $"""
            <!doctype html>
            <html lang="es">
            <body style="font-family:Arial,sans-serif;color:#263238;line-height:1.5">
              <p>Estimado/a {cliente},</p>
              <p>Le compartimos la factura correspondiente a su compra realizada en <strong>{empresa}</strong>.</p>
              <p>
                <strong>Número de factura:</strong> {numeroFactura}<br>
                <strong>Fecha:</strong> {factura.FechaEmision:dd/MM/yyyy}<br>
                <strong>Total:</strong> L. {factura.Total:N2}
              </p>
              <p>El detalle completo se encuentra en el archivo PDF adjunto.</p>
              <p>Gracias por su preferencia.</p>
            </body>
            </html>
            """;

        var (exito, error) = await _emailService.EnviarAsync(
            destinatario,
            asunto,
            cuerpo,
            new List<AdjuntoCorreo>
            {
                new()
                {
                    NombreArchivo = $"{factura.NumeroFactura}.pdf",
                    Contenido = pdf,
                    ContentType = "application/pdf"
                }
            });

        await RegistrarIntentoAsync(facturaId, new RegistrarEnvioDto
        {
            Canal = "Correo",
            Destinatario = destinatario,
            Resultado = exito ? "Enviado" : "Error",
            Error = error
        });

        return (exito, exito ? "Correo enviado correctamente." : (error ?? "No se pudo enviar el correo."));
    }

    private async Task<FacturaDto> ObtenerFacturaCompartibleAsync(int facturaId)
    {
        var factura = await _facturaService.GetByIdAsync(facturaId)
            ?? throw new BusinessRuleException("La factura no existe o no está disponible para el usuario actual.");

        if (string.Equals(factura.Estado, "Anulada", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("No se puede compartir una factura anulada.");

        return factura;
    }

    private string ResolverBackendPublicUrl()
    {
        var configurada = _configuration["AppSettings:BackendPublicUrl"]?.Trim().TrimEnd('/');
        if (EsUrlPublicaValida(configurada))
            return configurada!;

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request is not null && request.Host.HasValue)
        {
            var inferida = $"{request.Scheme}://{request.Host}".TrimEnd('/');
            if (EsUrlPublicaValida(inferida))
                return inferida;
        }

        throw new BusinessRuleException(
            "No se ha configurado una URL pública válida para el backend. Revisa AppSettings:BackendPublicUrl.");
    }

    private static bool EsUrlPublicaValida(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || !Uri.TryCreate(valor, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            return false;

        return !uri.IsLoopback &&
               !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
               !uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizarTelefonoHonduras(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono)) return string.Empty;

        var soloDigitos = new string(telefono.Where(char.IsDigit).ToArray());
        if (soloDigitos.StartsWith("00", StringComparison.Ordinal))
            soloDigitos = soloDigitos[2..];
        if (soloDigitos.Length == 8)
            return "504" + soloDigitos;

        return soloDigitos;
    }

    private static string GenerarTokenSeguro()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string CalcularHashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static bool EsCorreoValido(string correo) =>
        MailAddress.TryCreate(correo, out var direccion) &&
        string.Equals(direccion.Address, correo, StringComparison.OrdinalIgnoreCase);

    private static string? Truncar(string? valor, int longitudMaxima)
    {
        if (string.IsNullOrEmpty(valor)) return valor;
        return valor.Length <= longitudMaxima ? valor : valor[..longitudMaxima];
    }
}