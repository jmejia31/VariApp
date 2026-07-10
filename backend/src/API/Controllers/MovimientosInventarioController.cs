using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("inventario/movimientos")]
public class MovimientosInventarioController : ControllerBase
{
    private readonly IMovimientoInventarioService _service;

    public MovimientosInventarioController(IMovimientoInventarioService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetFiltered(
        [FromQuery] int? productoId, [FromQuery] string? tipo,
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var movimientos = await _service.GetFilteredAsync(productoId, tipo, desde, hasta);
        return Ok(ApiResponse<List<MovimientoInventarioDto>>.Ok(movimientos));
    }
}
