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
    private readonly IFacturaCompartirRepository _repository;
    private readonly IFacturaService _facturaService;
    private readonly IFacturaPdfService _facturaPdfService;
    private readonly IEmailService _emailService;
    private readonly IAuditoriaService _auditoria;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FacturaCompartirService(
        IFacturaCompartirRepository repository, IFacturaService facturaService, IFacturaPdfService facturaPdfService,
        IEmailService emailService, IAuditoriaService auditoria, ICurrentUserService currentUser,
        IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
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
        var factura = await _facturaService.GetByIdAsync(facturaId)
            ?? throw new BusinessRuleException("La factura no existe.");

        // Sección 14, punto 17: "validar que la factura exista y esté
        // autorizada antes de compartirla". Una factura anulada no debería
        // compartirse como si fuera válida para el cliente.
        if (factura.Estado == "Anulada")
            throw new BusinessRuleException("No se puede compartir una factura anulada.");

        var enlace = await _repository.GetEnlaceVigenteAsync(facturaId);
        if (enlace is null)
        {
            var diasValidez = _configuration.GetValue<int?>("AppSettings:EnlacePublicoFacturaDiasValidez") ?? 7;
            enlace = new EnlacePublicoFactura
            {
                Token = Guid.NewGuid().ToString("N"),
                FacturaId = facturaId,
                FechaExpiracion = DateTime.UtcNow.AddDays(diasValidez),
                CreadoPorUsuarioId = _currentUser.UsuarioId
            };
            await _repository.AddEnlaceAsync(enlace);
            await _repository.SaveChangesAsync();
        }

        var backendUrl = ResolverBackendPublicUrl();
        var urlPublica = $"{backendUrl}/facturas/publico/{enlace.Token}/pdf";

        // Plantilla EXACTA pedida en la sección 14 del prompt.
        var mensaje =
            $"Estimado/a {factura.ClienteNombre}, le compartimos la factura correspondiente a su compra " +
            $"realizada en {factura.EmpresaNombre}. Número de factura: {factura.NumeroFactura}. " +
            $"Total: L. {factura.Total:N2}. Puede descargarla aquí: {urlPublica} Gracias por su preferencia.";

        return new EnlaceCompartirDto
        {
            UrlPdfPublica = urlPublica,
            FechaExpiracion = enlace.FechaExpiracion,
            MensajeWhatsApp = mensaje,
            TelefonoSugerido = NormalizarTelefonoHonduras(factura.ClienteTelefono)
        };
    }

    /// Normaliza a formato internacional con código de país de Honduras
    /// (504) cuando el número no lo trae ya. Sección 14, puntos 2-4:
    /// "utilizar el número registrado", "permitir modificarlo",
    /// "utilizar código de país".
    private static string NormalizarTelefonoHonduras(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono)) return "";

        var soloDigitos = new string(telefono.Where(char.IsDigit).ToArray());
        if (soloDigitos.Length == 8) return "504" + soloDigitos; // número local hondureño
        return soloDigitos; // ya trae código de país u otro formato; se deja para que el usuario lo revise
    }

    private string ResolverBackendPublicUrl()
    {
        var configurada = _configuration["AppSettings:BackendPublicUrl"]?.Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(configurada) &&
            !configurada.Contains("localhost", StringComparison.OrdinalIgnoreCase) &&
            !configurada.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            return configurada;
        }

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request is not null && request.Host.HasValue)
        {
            return $"{request.Scheme}://{request.Host}".TrimEnd('/');
        }

        return configurada ?? "";
    }

    public async Task RegistrarIntentoAsync(int facturaId, RegistrarEnvioDto dto)
    {
        await _repository.AddHistorialAsync(new HistorialEnvioFactura
        {
            FacturaId = facturaId,
            Canal = dto.Canal,
            Destinatario = dto.Destinatario,
            Resultado = dto.Resultado,
            Error = dto.Error,
            UsuarioId = _currentUser.UsuarioId,
            UsuarioNombre = _currentUser.NombreUsuario
        });
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Facturacion, AccionPermiso.Compartir,
            $"Intento de envío de factura por {dto.Canal} a '{dto.Destinatario}': {dto.Resultado}.",
            facturaId, entidad: "Factura", resultado: dto.Resultado == "Error" ? "Error" : "Exito", error: dto.Error);
    }

    public async Task<List<HistorialEnvioDto>> GetHistorialAsync(int facturaId)
    {
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

    public async Task<(byte[] Pdf, string NombreArchivo)?> ObtenerPdfPorTokenAsync(string token)
    {
        var enlace = await _repository.GetPorTokenAsync(token);
        if (enlace is null || enlace.FechaExpiracion < DateTime.UtcNow) return null;

        var factura = await _facturaService.GetByIdAsync(enlace.FacturaId);
        if (factura is null) return null;

        enlace.VecesAccedido += 1;
        enlace.UltimoAcceso = DateTime.UtcNow;
        _repository.UpdateEnlace(enlace);
        await _repository.SaveChangesAsync();

        var pdf = await _facturaPdfService.GenerarPdfAsync(factura);
        return (pdf, $"{factura.NumeroFactura}.pdf");
    }

    public async Task<(bool Exito, string Mensaje)> EnviarPorCorreoAsync(int facturaId, string destinatario)
    {
        if (!EsCorreoValido(destinatario))
            return (false, "El correo indicado no tiene un formato válido.");

        var factura = await _facturaService.GetByIdAsync(facturaId)
            ?? throw new BusinessRuleException("La factura no existe.");

        if (factura.Estado == "Anulada")
            throw new BusinessRuleException("No se puede compartir una factura anulada.");

        var pdf = await _facturaPdfService.GenerarPdfAsync(factura);

        var asunto = $"Factura {factura.NumeroFactura} - {factura.EmpresaNombre}";
        var cuerpo = $"""
            <p>Estimado/a {factura.ClienteNombre},</p>
            <p>Le compartimos la factura correspondiente a su compra realizada en <strong>{factura.EmpresaNombre}</strong>.</p>
            <p><strong>Número de factura:</strong> {factura.NumeroFactura}<br>
            <strong>Fecha:</strong> {factura.FechaEmision:dd/MM/yyyy}<br>
            <strong>Total:</strong> L. {factura.Total:N2}</p>
            <p>Encontrará el detalle completo en el archivo PDF adjunto.</p>
            <p>Gracias por su preferencia.</p>
            """;

        var (exito, error) = await _emailService.EnviarAsync(destinatario, asunto, cuerpo, new List<AdjuntoCorreo>
        {
            new() { NombreArchivo = $"{factura.NumeroFactura}.pdf", Contenido = pdf, ContentType = "application/pdf" }
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

    private static bool EsCorreoValido(string correo)
    {
        if (string.IsNullOrWhiteSpace(correo)) return false;
        try
        {
            var direccion = new System.Net.Mail.MailAddress(correo);
            return direccion.Address == correo.Trim();
        }
        catch
        {
            return false;
        }
    }
}
