using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

/// La factura se genera y anula automáticamente junto con su venta origen.
/// Descargar, imprimir, WhatsApp y correo consumen el mismo PDF oficial.
[ApiController]
[Authorize]
[Route("facturas")]
public class FacturasController : ControllerBase
{
    private readonly IFacturaService _facturaService;
    private readonly IFacturaPdfService _facturaPdfService;
    private readonly IFacturaCompartirService _facturaCompartirService;
    private readonly IAuditoriaService _auditoria;

    public FacturasController(
        IFacturaService facturaService,
        IFacturaPdfService facturaPdfService,
        IFacturaCompartirService facturaCompartirService,
        IAuditoriaService auditoria)
    {
        _facturaService = facturaService;
        _facturaPdfService = facturaPdfService;
        _facturaCompartirService = facturaCompartirService;
        _auditoria = auditoria;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll()
    {
        var facturas = await _facturaService.GetAllAsync();
        return Ok(ApiResponse<List<FacturaDto>>.Ok(facturas));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var factura = await _facturaService.GetByIdAsync(id);
        if (factura is null) return NotFound(ApiResponse<object>.Fail("Factura no encontrada."));
        return Ok(ApiResponse<FacturaDto>.Ok(factura));
    }

    [HttpGet("venta/{ventaId:int}")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetByVenta(int ventaId)
    {
        var factura = await _facturaService.GetByVentaIdAsync(ventaId);
        if (factura is null) return NotFound(ApiResponse<object>.Fail("Esta venta no tiene factura generada."));
        return Ok(ApiResponse<FacturaDto>.Ok(factura));
    }

    [HttpGet("{id:int}/pdf")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Exportar)]
    public async Task<IActionResult> DescargarPdf(int id)
    {
        var factura = await _facturaService.GetByIdAsync(id);
        if (factura is null) return NotFound(ApiResponse<object>.Fail("Factura no encontrada."));

        AplicarEncabezadosDocumentoPrivado();
        var pdfBytes = await _facturaPdfService.GenerarPdfAsync(factura);
        await _auditoria.RegistrarAsync(
            ModuloSistema.Facturacion,
            AccionPermiso.Exportar,
            $"PDF descargado de factura: {factura.NumeroFactura}.",
            id,
            entidad: "Factura",
            valoresNuevos: new { factura.NumeroFactura, factura.Total });

        return File(pdfBytes, "application/pdf", $"{factura.NumeroFactura}.pdf");
    }

    [HttpPost("{id:int}/compartir/whatsapp")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Compartir)]
    public async Task<IActionResult> PrepararWhatsApp(int id)
    {
        var enlace = await _facturaCompartirService.PrepararCompartirAsync(id);
        return Ok(ApiResponse<EnlaceCompartirDto>.Ok(
            enlace,
            "Enlace temporal creado. Cualquier enlace anterior de esta factura fue revocado."));
    }

    [HttpPost("{id:int}/compartir/revocar")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Compartir)]
    public async Task<IActionResult> RevocarEnlaces(int id)
    {
        var revocados = await _facturaCompartirService.RevocarEnlacesAsync(id);
        return Ok(ApiResponse<object>.Ok(
            new { enlacesRevocados = revocados },
            revocados > 0
                ? "Los enlaces públicos vigentes fueron revocados."
                : "La factura no tenía enlaces públicos vigentes."));
    }

    [HttpPost("{id:int}/compartir/registrar")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Compartir)]
    public async Task<IActionResult> RegistrarIntento(int id, [FromBody] RegistrarEnvioDto dto)
    {
        await _facturaCompartirService.RegistrarIntentoAsync(id, dto);
        return Ok(ApiResponse<object>.Ok(new { }, "Intento registrado."));
    }

    [HttpGet("{id:int}/historial-envios")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetHistorialEnvios(int id)
    {
        var historial = await _facturaCompartirService.GetHistorialAsync(id);
        return Ok(ApiResponse<List<HistorialEnvioDto>>.Ok(historial));
    }

    [HttpPost("{id:int}/compartir/correo")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Compartir)]
    public async Task<IActionResult> EnviarPorCorreo(int id, [FromBody] EnviarCorreoFacturaDto dto)
    {
        var (exito, mensaje) = await _facturaCompartirService.EnviarPorCorreoAsync(id, dto.Destinatario);
        if (!exito)
            return BadRequest(ApiResponse<object>.Fail(mensaje));

        return Ok(ApiResponse<object>.Ok(new { }, mensaje));
    }

    /// Descarga pública deliberada para destinatarios externos. La seguridad
    /// depende de un token aleatorio cuyo hash está persistido, expiración,
    /// revocación y límite de accesos. El token nunca se registra en auditoría.
    [HttpGet("publico/{token}/pdf")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> DescargarPdfPublico(string token)
    {
        AplicarEncabezadosDocumentoPublico();
        var resultado = await _facturaCompartirService.ObtenerPdfPorTokenAsync(token);
        if (resultado is null)
            return NotFound(ApiResponse<object>.Fail("Este enlace no es válido, fue revocado, alcanzó su límite o ya expiró."));

        return File(resultado.Value.Pdf, "application/pdf", resultado.Value.NombreArchivo);
    }

    private void AplicarEncabezadosDocumentoPrivado()
    {
        Response.Headers["Cache-Control"] = "private, no-store, no-cache, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
    }

    private void AplicarEncabezadosDocumentoPublico()
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["X-Frame-Options"] = "DENY";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; sandbox";
    }
}