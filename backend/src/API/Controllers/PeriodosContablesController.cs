using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs.Contabilidad;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("periodos-contables")]
public sealed class PeriodosContablesController : ControllerBase
{
    private readonly IPeriodoContableService _service;

    public PeriodosContablesController(AppDbContext db, IAuditoriaService auditoria)
    {
        _service = new PeriodoContableService(new PeriodoContableRepository(db), auditoria);
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll() =>
        Ok(ApiResponse<List<PeriodoContableDto>>.Ok(await _service.GetAllAsync()));

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var periodo = await _service.GetByIdAsync(id);
        return periodo is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Período contable no encontrado", detail: $"No existe un período contable con Id {id}.")
            : Ok(ApiResponse<PeriodoContableDto>.Ok(periodo));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CrearPeriodoContableDto dto)
    {
        try
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<PeriodoContableDto>.Ok(created));
        }
        catch (ArgumentException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Período contable inválido", detail: ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflicto de período contable", detail: ex.Message);
        }
    }

    [HttpPost("{id:int}/cerrar")]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Cerrar)]
    public async Task<IActionResult> Cerrar(int id)
    {
        try
        {
            await _service.CerrarAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Período contable no encontrado", detail: ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Transición de estado inválida", detail: ex.Message);
        }
    }
}
