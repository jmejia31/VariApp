using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

/// La factura se genera y anula automáticamente junto con su venta origen
/// (ver VentasController: /ventas/{id}/confirmar y /ventas/{id}/anular).
/// Este controlador es solo de consulta.
[ApiController]
[Authorize]
[Route("facturas")]
public class FacturasController : ControllerBase
{
    private readonly IFacturaService _facturaService;
    private readonly IFacturaPdfService _facturaPdfService;
    private readonly IFacturaCompartirService _facturaCompartirService;

    public FacturasController(IFacturaService facturaService, IFacturaPdfService facturaPdfService, IFacturaCompartirService facturaCompartirService)
    {
        _facturaService = facturaService;
        _facturaPdfService = facturaPdfService;
        _facturaCompartirService = facturaCompartirService;
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

    /// PDF real generado en backend (sección 13/14: nunca la vista HTML
    /// imprimible como sustituto final). Protegido con Exportar según el
    /// mapeo acción->permiso de la sección 10 ("Descargar factura").
    [HttpGet("{id:int}/pdf")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Exportar)]
    public async Task<IActionResult> DescargarPdf(int id)
    {
        var factura = await _facturaService.GetByIdAsync(id);
        if (factura is null) return NotFound(ApiResponse<object>.Fail("Factura no encontrada."));

        var pdfBytes = await _facturaPdfService.GenerarPdfAsync(factura);
        return File(pdfBytes, "application/pdf", $"{factura.NumeroFactura}.pdf");
    }

    /// Genera (o reutiliza) el enlace público temporal + mensaje de WhatsApp
    /// listo para usar (sección 14).
    [HttpPost("{id:int}/compartir/whatsapp")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Compartir)]
    public async Task<IActionResult> PrepararWhatsApp(int id)
    {
        var enlace = await _facturaCompartirService.PrepararCompartirAsync(id);
        return Ok(ApiResponse<EnlaceCompartirDto>.Ok(enlace));
    }

    /// Registra el intento de envío (sección 14/18: el frontend llama esto
    /// justo antes/después de abrir wa.me, ya que no hay forma de confirmar
    /// la entrega real sin una API oficial de WhatsApp — limitación real,
    /// no simulada).
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

    /// Endpoint PÚBLICO deliberado (sin JWT): es la URL que se abre desde
    /// WhatsApp o un cliente de correo externo, que no tiene sesión en el
    /// sistema. Su seguridad no depende de [Authorize] sino de que el token
    /// sea impredecible (GUID) y expire (ver EnlacePublicoFactura).
    [HttpGet("publico/{token}/pdf")]
    [AllowAnonymous]
    public async Task<IActionResult> DescargarPdfPublico(string token)
    {
        var resultado = await _facturaCompartirService.ObtenerPdfPorTokenAsync(token);
        if (resultado is null)
            return NotFound(ApiResponse<object>.Fail("Este enlace no es válido o ya expiró. Solicita uno nuevo."));

        return File(resultado.Value.Pdf, "application/pdf", resultado.Value.NombreArchivo);
    }
}
