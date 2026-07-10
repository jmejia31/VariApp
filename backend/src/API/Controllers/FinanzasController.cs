using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("finanzas")]
public class FinanzasController : ControllerBase
{
    private readonly IFinanzasService _finanzasService;

    public FinanzasController(IFinanzasService finanzasService)
    {
        _finanzasService = finanzasService;
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> GetResumen()
    {
        var resumen = await _finanzasService.GetResumenAsync();
        return Ok(ApiResponse<FinanzasResumenDto>.Ok(resumen));
    }

    [HttpGet("movimientos")]
    public async Task<IActionResult> GetMovimientos([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var movimientos = await _finanzasService.GetMovimientosAsync(desde, hasta);
        return Ok(ApiResponse<List<MovimientoFinancieroDto>>.Ok(movimientos));
    }

    [HttpPost("movimientos/manual")]
    public async Task<IActionResult> RegistrarManual([FromBody] CreateMovimientoManualDto dto)
    {
        var creado = await _finanzasService.RegistrarMovimientoManualAsync(dto);
        return Ok(ApiResponse<MovimientoFinancieroDto>.Ok(creado, "Movimiento registrado correctamente."));
    }

    [HttpPost("movimientos/{id:int}/anular")]
    public async Task<IActionResult> AnularMovimiento(int id, [FromBody] AnularDocumentoDto dto)
    {
        var anulado = await _finanzasService.AnularMovimientoAsync(id, dto.MotivoAnulacion);
        if (anulado is null) return NotFound(ApiResponse<object>.Fail("Movimiento no encontrado."));
        return Ok(ApiResponse<MovimientoFinancieroDto>.Ok(anulado, "Movimiento anulado correctamente."));
    }

    [HttpGet("revisiones")]
    public async Task<IActionResult> GetRevisiones()
    {
        var revisiones = await _finanzasService.GetRevisionesAsync();
        return Ok(ApiResponse<List<RevisionFinancieraDto>>.Ok(revisiones));
    }

    [HttpPost("revisiones")]
    public async Task<IActionResult> RegistrarRevision([FromBody] CreateRevisionFinancieraDto dto)
    {
        var creada = await _finanzasService.RegistrarRevisionAsync(dto);
        return Ok(ApiResponse<RevisionFinancieraDto>.Ok(creada, "Revisión financiera registrada correctamente."));
    }
}
