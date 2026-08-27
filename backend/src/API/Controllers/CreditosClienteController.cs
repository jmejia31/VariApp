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
[Route("creditos-clientes")]
public sealed class CreditosClienteController : ControllerBase
{
    private readonly ICreditoClienteService _service;

    public CreditosClienteController(ICreditoClienteService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Clientes, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return Ok(ApiResponse<CreditoClienteDto>.Ok(item));
    }

    [HttpGet("cliente/{clienteId:int}")]
    [RequierePermiso(ModuloSistema.Clientes, AccionPermiso.Ver)]
    public async Task<IActionResult> GetByCliente(int clienteId)
    {
        var items = await _service.GetByClienteIdAsync(clienteId);
        return Ok(ApiResponse<IReadOnlyList<CreditoClienteDto>>.Ok(items));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Clientes, AccionPermiso.Crear)]
    public async Task<IActionResult> Crear([FromBody] CreateCreditoClienteDto dto)
    {
        var creado = await _service.CrearAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, ApiResponse<CreditoClienteDto>.Ok(creado, "Configuración de crédito creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Clientes, AccionPermiso.Editar)]
    public async Task<IActionResult> ActualizarPolitica(int id, [FromBody] UpdateCreditoClienteDto dto)
    {
        var actualizado = await _service.ActualizarPoliticaAsync(id, dto);
        return Ok(ApiResponse<CreditoClienteDto>.Ok(actualizado, "Política de crédito actualizada correctamente."));
    }
}
