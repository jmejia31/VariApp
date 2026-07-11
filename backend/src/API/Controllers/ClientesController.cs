using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("clientes")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _service;

    public ClientesController(IClienteService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clientes = await _service.GetAllAsync();
        return Ok(ApiResponse<List<ClienteDto>>.Ok(clientes));
    }

    [HttpGet("activos")]
    public async Task<IActionResult> GetActivos()
    {
        var clientes = await _service.GetActivosAsync();
        return Ok(ApiResponse<List<ClienteDto>>.Ok(clientes));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var cliente = await _service.GetByIdAsync(id);
        if (cliente is null) return NotFound(ApiResponse<object>.Fail("Cliente no encontrado."));
        return Ok(ApiResponse<ClienteDto>.Ok(cliente));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClienteDto dto)
    {
        var creado = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id },
            ApiResponse<ClienteDto>.Ok(creado, "Cliente creado correctamente."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClienteDto dto)
    {
        var actualizado = await _service.UpdateAsync(id, dto);
        if (actualizado is null) return NotFound(ApiResponse<object>.Fail("Cliente no encontrado."));
        return Ok(ApiResponse<ClienteDto>.Ok(actualizado, "Cliente actualizado correctamente."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _service.DeleteAsync(id);
        if (!eliminado) return NotFound(ApiResponse<object>.Fail("Cliente no encontrado."));
        return Ok(ApiResponse<object>.Ok(new { }, "Cliente eliminado correctamente."));
    }
}
