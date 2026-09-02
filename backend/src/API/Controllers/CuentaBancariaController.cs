using InventoryApp.API.Filters;
using InventoryApp.Application.DTOs.Bancos;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("cuentas-bancarias")]
public class CuentaBancariaController : ControllerBase
{
    private readonly ICuentaBancariaService _service;

    public CuentaBancariaController(ICuentaBancariaService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("activas")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetActivas()
    {
        var result = await _service.GetActivasAsync();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateCuentaBancariaDto dto)
    {
        var result = await _service.AddAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Activar)]
    public async Task<IActionResult> Activar(int id)
    {
        await _service.ActivarAsync(id);
        return NoContent();
    }

    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Desactivar)]
    public async Task<IActionResult> Desactivar(int id)
    {
        await _service.DesactivarAsync(id);
        return NoContent();
    }
}
