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
[Route("inventario/movimientos")]
public class MovimientosInventarioController : ControllerBase
{
    private readonly IMovimientoInventarioService _service;

    public MovimientosInventarioController(IMovimientoInventarioService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetFiltered(
        [FromQuery] int? productoId, [FromQuery] string? tipo,
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var movimientos = await _service.GetFilteredAsync(productoId, tipo, desde, hasta);
        return Ok(ApiResponse<List<MovimientoInventarioDto>>.Ok(movimientos));
    }
}
