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
[Route("costos-envio")]
public class CostosEnvioController : ControllerBase
{
    private readonly ICostoEnvioService _service;
    private readonly IAuditoriaService _auditoria;

    public CostosEnvioController(ICostoEnvioService service, IAuditoriaService auditoria)
    {
        _service = service;
        _auditoria = auditoria;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll() =>
        Ok(ApiResponse<List<CostoEnvioDto>>.Ok(await _service.GetAllAsync()));

    [HttpGet("predeterminado")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetPredeterminado()
    {
        var item = await _service.GetPredeterminadoVigenteAsync();
        return item is null
            ? NotFound(ApiResponse<object>.Fail("No existe un costo de envío predeterminado vigente."))
            : Ok(ApiResponse<CostoEnvioDto>.Ok(item));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null
            ? NotFound(ApiResponse<object>.Fail("Costo de envío no encontrado."))
            : Ok(ApiResponse<CostoEnvioDto>.Ok(item));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Administrar)]
    public async Task<IActionResult> Create([FromBody] GuardarCostoEnvioDto dto)
    {
        var item = await _service.CreateAsync(dto);
        await _auditoria.RegistrarAsync(ModuloSistema.Facturacion, AccionPermiso.Crear,
            $"Costo de envío creado: {item.Nombre}.", item.Id, entidad: "CostoEnvio", valoresNuevos: item);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ApiResponse<CostoEnvioDto>.Ok(item));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Administrar)]
    public async Task<IActionResult> Update(int id, [FromBody] GuardarCostoEnvioDto dto)
    {
        var item = await _service.UpdateAsync(id, dto);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Costo de envío no encontrado."));
        await _auditoria.RegistrarAsync(ModuloSistema.Facturacion, AccionPermiso.Editar,
            $"Costo de envío actualizado: {item.Nombre}.", item.Id, entidad: "CostoEnvio", valoresNuevos: item);
        return Ok(ApiResponse<CostoEnvioDto>.Ok(item));
    }

    [HttpPatch("{id:int}/estado")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Administrar)]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoCostoEnvioDto dto)
    {
        if (!await _service.CambiarEstadoAsync(id, dto.Activo))
            return NotFound(ApiResponse<object>.Fail("Costo de envío no encontrado."));
        await _auditoria.RegistrarAsync(ModuloSistema.Facturacion,
            dto.Activo ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"Costo de envío {(dto.Activo ? "activado" : "desactivado")}.", id, entidad: "CostoEnvio");
        return Ok(ApiResponse<object>.Ok(new { id, dto.Activo }));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Administrar)]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _service.EliminarAsync(id))
            return NotFound(ApiResponse<object>.Fail("Costo de envío no encontrado."));
        await _auditoria.RegistrarAsync(ModuloSistema.Facturacion, AccionPermiso.EliminarLogico,
            "Costo de envío eliminado lógicamente.", id, entidad: "CostoEnvio");
        return Ok(ApiResponse<object>.Ok(new { id }));
    }
}

public class CambiarEstadoCostoEnvioDto
{
    public bool Activo { get; set; }
}
