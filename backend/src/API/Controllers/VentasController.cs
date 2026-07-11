using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("ventas")]
public class VentasController : ControllerBase
{
    private readonly IVentaService _ventaService;

    public VentasController(IVentaService ventaService)
    {
        _ventaService = ventaService;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetPaged([FromQuery] PagedRequest request)
    {
        var resultado = await _ventaService.GetPagedAsync(request);
        return Ok(ApiResponse<PagedResult<VentaDto>>.Ok(resultado));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var venta = await _ventaService.GetByIdAsync(id);
        if (venta is null) return NotFound(ApiResponse<object>.Fail("Venta no encontrada."));
        return Ok(ApiResponse<VentaDto>.Ok(venta));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateVentaDto dto)
    {
        var creada = await _ventaService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creada.Id },
            ApiResponse<VentaDto>.Ok(creada, "Venta creada en estado Borrador."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVentaDto dto)
    {
        var actualizada = await _ventaService.UpdateAsync(id, dto);
        if (actualizada is null) return NotFound(ApiResponse<object>.Fail("Venta no encontrada."));
        return Ok(ApiResponse<VentaDto>.Ok(actualizada, "Venta actualizada correctamente."));
    }

    [HttpPost("{id:int}/confirmar")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Confirmar(int id)
    {
        var confirmada = await _ventaService.ConfirmarAsync(id);
        if (confirmada is null) return NotFound(ApiResponse<object>.Fail("Venta no encontrada."));
        return Ok(ApiResponse<VentaDto>.Ok(confirmada, "Venta confirmada: stock actualizado y factura generada."));
    }

    [HttpPost("{id:int}/anular")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Anular)]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularDocumentoDto dto)
    {
        var anulada = await _ventaService.AnularAsync(id, dto.MotivoAnulacion);
        if (anulada is null) return NotFound(ApiResponse<object>.Fail("Venta no encontrada."));
        return Ok(ApiResponse<VentaDto>.Ok(anulada, "Venta anulada: stock revertido y factura anulada."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Eliminar)]
    public async Task<IActionResult> DeleteBorrador(int id)
    {
        var eliminada = await _ventaService.DeleteBorradorAsync(id);
        if (!eliminada) return NotFound(ApiResponse<object>.Fail("Venta no encontrada."));
        return Ok(ApiResponse<object>.Ok(new { }, "Venta (borrador) eliminada."));
    }
}
