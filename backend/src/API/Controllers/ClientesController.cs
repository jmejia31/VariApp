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
[Route("clientes")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _service;

    public ClientesController(IClienteService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Clientes, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll()
    {
        var clientes = await _service.GetAllAsync();
        return Ok(ApiResponse<List<ClienteDto>>.Ok(clientes));
    }

    [HttpGet("activos")]
    [RequierePermiso(ModuloSistema.Clientes, AccionPermiso.Ver)]
    public async Task<IActionResult> GetActivos()
    {
        var clientes = await _service.GetActivosAsync();
        return Ok(ApiResponse<List<ClienteDto>>.Ok(clientes));
    }

    [HttpGet("buscar")]
    [RequierePermiso(ModuloSistema.Clientes, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] string termino)
    {
        var clientes = await _service.BuscarActivosAsync(termino);
        return Ok(ApiResponse<List<ClienteDto>>.Ok(clientes));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Clientes, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var cliente = await _service.GetByIdAsync(id);
        if (cliente is null) return NotFound(ApiResponse<object>.Fail("Cliente no encontrado."));
        return Ok(ApiResponse<ClienteDto>.Ok(cliente));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Clientes, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateClienteDto dto)
    {
        var creado = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id },
            ApiResponse<ClienteDto>.Ok(creado, "Cliente creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Clientes, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClienteDto dto)
    {
        var actualizado = await _service.UpdateAsync(id, dto);
        if (actualizado is null) return NotFound(ApiResponse<object>.Fail("Cliente no encontrado."));
        return Ok(ApiResponse<ClienteDto>.Ok(actualizado, "Cliente actualizado correctamente."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Clientes, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _service.DeleteAsync(id);
        if (!eliminado) return NotFound(ApiResponse<object>.Fail("Cliente no encontrado."));
        return Ok(ApiResponse<object>.Ok(new { }, "Cliente eliminado correctamente."));
    }
}
