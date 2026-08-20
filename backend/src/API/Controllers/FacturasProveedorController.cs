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
[Route("facturas-proveedor")]
public sealed class FacturasProveedorController : ControllerBase
{
    private readonly IFacturaProveedorService _service;

    public FacturasProveedorController(IFacturaProveedorService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] FacturaProveedorFiltroDto filtro)
    {
        var pagina = await _service.GetPagedAsync(filtro);
        return Ok(ApiResponse<PagedResult<FacturaProveedorDto>>.Ok(pagina));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var factura = await _service.GetByIdAsync(id);
        return factura is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Factura de proveedor no encontrada", detail: "No existe una factura de proveedor con el identificador indicado.")
            : Ok(ApiResponse<FacturaProveedorDto>.Ok(factura));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateFacturaProveedorDto dto)
    {
        var creada = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creada.Id },
            ApiResponse<FacturaProveedorDto>.Ok(creada, "Factura de proveedor creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFacturaProveedorDto dto)
    {
        var actualizada = await _service.UpdateAsync(id, dto);
        return Ok(ApiResponse<FacturaProveedorDto>.Ok(actualizada, "Factura de proveedor actualizada correctamente."));
    }

    [HttpPost("{id:int}/registrar")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Registrar(int id)
    {
        var registrada = await _service.RegistrarAsync(id);
        return Ok(ApiResponse<FacturaProveedorDto>.Ok(registrada, "Factura de proveedor registrada correctamente."));
    }

    [HttpPost("{id:int}/anular")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Anular)]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularFacturaProveedorDto dto)
    {
        var anulada = await _service.AnularAsync(id, dto);
        return Ok(ApiResponse<FacturaProveedorDto>.Ok(anulada, "Factura de proveedor anulada correctamente."));
    }
}
