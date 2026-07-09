using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("compras")]
public class ComprasController : ControllerBase
{
    private readonly ICompraService _compraService;

    public ComprasController(ICompraService compraService)
    {
        _compraService = compraService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] PagedRequest request)
    {
        var resultado = await _compraService.GetPagedAsync(request);
        return Ok(ApiResponse<PagedResult<CompraDto>>.Ok(resultado));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var compra = await _compraService.GetByIdAsync(id);
        if (compra is null) return NotFound(ApiResponse<object>.Fail("Compra no encontrada."));
        return Ok(ApiResponse<CompraDto>.Ok(compra));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCompraDto dto)
    {
        var creada = await _compraService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creada.Id },
            ApiResponse<CompraDto>.Ok(creada, "Compra creada en estado Borrador."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCompraDto dto)
    {
        var actualizada = await _compraService.UpdateAsync(id, dto);
        if (actualizada is null) return NotFound(ApiResponse<object>.Fail("Compra no encontrada."));
        return Ok(ApiResponse<CompraDto>.Ok(actualizada, "Compra actualizada correctamente."));
    }

    [HttpPost("{id:int}/confirmar")]
    public async Task<IActionResult> Confirmar(int id)
    {
        var confirmada = await _compraService.ConfirmarAsync(id);
        if (confirmada is null) return NotFound(ApiResponse<object>.Fail("Compra no encontrada."));
        return Ok(ApiResponse<CompraDto>.Ok(confirmada, "Compra confirmada: stock actualizado."));
    }

    [HttpPost("{id:int}/anular")]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularDocumentoDto dto)
    {
        var anulada = await _compraService.AnularAsync(id, dto.MotivoAnulacion);
        if (anulada is null) return NotFound(ApiResponse<object>.Fail("Compra no encontrada."));
        return Ok(ApiResponse<CompraDto>.Ok(anulada, "Compra anulada: stock revertido."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBorrador(int id)
    {
        var eliminada = await _compraService.DeleteBorradorAsync(id);
        if (!eliminada) return NotFound(ApiResponse<object>.Fail("Compra no encontrada."));
        return Ok(ApiResponse<object>.Ok(new { }, "Compra (borrador) eliminada."));
    }
}
