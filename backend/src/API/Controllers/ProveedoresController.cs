using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("proveedores")]
public class ProveedoresController : ControllerBase
{
    private readonly IProveedorService _service;

    public ProveedoresController(IProveedorService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var proveedores = await _service.GetAllAsync();
        return Ok(ApiResponse<List<ProveedorDto>>.Ok(proveedores));
    }

    [HttpGet("activos")]
    public async Task<IActionResult> GetActivos()
    {
        var proveedores = await _service.GetActivosAsync();
        return Ok(ApiResponse<List<ProveedorDto>>.Ok(proveedores));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var proveedor = await _service.GetByIdAsync(id);
        if (proveedor is null) return NotFound(ApiResponse<object>.Fail("Proveedor no encontrado."));
        return Ok(ApiResponse<ProveedorDto>.Ok(proveedor));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProveedorDto dto)
    {
        var creado = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id },
            ApiResponse<ProveedorDto>.Ok(creado, "Proveedor creado correctamente."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProveedorDto dto)
    {
        var actualizado = await _service.UpdateAsync(id, dto);
        if (actualizado is null) return NotFound(ApiResponse<object>.Fail("Proveedor no encontrado."));
        return Ok(ApiResponse<ProveedorDto>.Ok(actualizado, "Proveedor actualizado correctamente."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _service.DeleteAsync(id);
        if (!eliminado) return NotFound(ApiResponse<object>.Fail("Proveedor no encontrado."));
        return Ok(ApiResponse<object>.Ok(new { }, "Proveedor eliminado correctamente."));
    }
}
