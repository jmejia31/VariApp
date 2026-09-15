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
[Route("productos")]
public sealed class InventarioAjustesController : ControllerBase
{
    private readonly IInventarioAjusteService _service;

    public InventarioAjustesController(IInventarioAjusteService service)
    {
        _service = service;
    }

    [HttpPost("{productoId:int}/ajustes-stock")]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Confirmar)]
    public async Task<IActionResult> AjustarProducto(
        int productoId,
        [FromBody] AjusteStockRequest request)
    {
        var resultado = await _service.AjustarProductoAsync(productoId, request);
        return Ok(ApiResponse<AjusteStockResultadoDto>.Ok(
            resultado,
            "Inventario del producto ajustado correctamente."));
    }

    [HttpPost("{productoId:int}/variantes/{varianteId:int}/ajustes-stock")]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Confirmar)]
    public async Task<IActionResult> AjustarVariante(
        int productoId,
        int varianteId,
        [FromBody] AjusteStockRequest request)
    {
        var resultado = await _service.AjustarVarianteAsync(
            productoId,
            varianteId,
            request);
        return Ok(ApiResponse<AjusteStockResultadoDto>.Ok(
            resultado,
            "Inventario de la variante ajustado correctamente."));
    }
}
