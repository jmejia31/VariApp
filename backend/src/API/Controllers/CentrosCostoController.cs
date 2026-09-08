using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs.Contabilidad;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities.Contabilidad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("centros-costo")]
public sealed class CentrosCostoController : ControllerBase
{
    private readonly ICentroCostoService _service;

    public CentrosCostoController(ICentroCostoService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Buscar([FromQuery] string? termino = null, [FromQuery] TipoCentroCosto? tipo = null, [FromQuery] int? sucursalId = null, [FromQuery] bool? activo = null, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        var (items, total) = await _service.BuscarAsync(termino, tipo, sucursalId, activo, pagina, tamanoPagina);
        return Ok(ApiResponse<object>.Ok(new { items, total, pagina = Math.Max(1, pagina), tamanoPagina = Math.Clamp(tamanoPagina, 1, 100) }));
    }

    [HttpGet("activos")]
    public async Task<IActionResult> GetActivos([FromQuery] TipoCentroCosto? tipo = null, [FromQuery] int? sucursalId = null)
        => Ok(ApiResponse<List<CentroCostoDto>>.Ok(await _service.GetActivosAsync(tipo, sucursalId)));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound(ApiResponse<object>.Fail("Centro de costo no encontrado.")) : Ok(ApiResponse<CentroCostoDto>.Ok(item));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCentroCostoDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<CentroCostoDto>.Ok(created, "Centro de costo creado correctamente."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCentroCostoDto dto)
    {
        var updated = await _service.UpdateAsync(id, dto);
        return updated is null ? NotFound(ApiResponse<object>.Fail("Centro de costo no encontrado.")) : Ok(ApiResponse<CentroCostoDto>.Ok(updated, "Centro de costo actualizado correctamente."));
    }

    [HttpPatch("{id:int}/activar")]
    public async Task<IActionResult> Activar(int id)
    {
        var item = await _service.CambiarEstadoAsync(id, true);
        return item is null ? NotFound(ApiResponse<object>.Fail("Centro de costo no encontrado.")) : Ok(ApiResponse<CentroCostoDto>.Ok(item));
    }

    [HttpPatch("{id:int}/desactivar")]
    public async Task<IActionResult> Desactivar(int id)
    {
        var item = await _service.CambiarEstadoAsync(id, false);
        return item is null ? NotFound(ApiResponse<object>.Fail("Centro de costo no encontrado.")) : Ok(ApiResponse<CentroCostoDto>.Ok(item));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? Ok(ApiResponse<object>.Ok(new { }, "Centro de costo eliminado correctamente.")) : NotFound(ApiResponse<object>.Fail("Centro de costo no encontrado."));
}
