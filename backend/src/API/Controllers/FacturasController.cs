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

    public FacturasController(IFacturaService facturaService, IFacturaPdfService facturaPdfService)
    {
        _facturaService = facturaService;
        _facturaPdfService = facturaPdfService;
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
}
