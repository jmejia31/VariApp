using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("reservas-inventario")]
public sealed class ReservasInventarioController : ControllerBase
{
    private readonly IReservaInventarioService _service;
    private readonly IAuditoriaService? _auditoria;

    public ReservasInventarioController(IReservaInventarioService service, IAuditoriaService? auditoria = null)
    {
        _service = service;
        _auditoria = auditoria;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] ReservaInventarioQueryDto query)
    {
        var pagina = await _service.GetPagedAsync(query);
        return Ok(ApiResponse<PagedResult<ReservaInventarioDto>>.Ok(pagina));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var reserva = await _service.GetByIdAsync(id);
        return reserva is null
            ? NotFound(ApiResponse<object>.Fail("Reserva de inventario no encontrada."))
            : Ok(ApiResponse<ReservaInventarioDto>.Ok(reserva));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateReservaInventarioDto dto)
    {
        var creada = await _service.CreateAsync(dto);
        await AuditarAsync(AccionPermiso.Crear, creada, "Reserva de inventario creada.");
        return CreatedAtAction(nameof(GetById), new { id = creada.Id },
            ApiResponse<ReservaInventarioDto>.Ok(creada, "Reserva creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReservaInventarioDto dto)
    {
        var reserva = await _service.UpdateAsync(id, dto);
        await AuditarAsync(AccionPermiso.Editar, reserva, "Reserva de inventario actualizada.");
        return Ok(ApiResponse<ReservaInventarioDto>.Ok(reserva, "Reserva actualizada correctamente."));
    }

    [HttpPost("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Confirmar)]
    public Task<IActionResult> Activar(int id) => EjecutarTransicionAsync(
        id, _service.ActivarAsync, AccionPermiso.Confirmar, "Reserva activada correctamente.");

    [HttpPost("{id:int}/consumir")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Confirmar)]
    public Task<IActionResult> Consumir(int id) => EjecutarTransicionAsync(
        id, _service.ConsumirAsync, AccionPermiso.Confirmar, "Reserva consumida correctamente.");

    [HttpPost("{id:int}/liberar")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Anular)]
    public async Task<IActionResult> Liberar(int id, [FromBody] LiberarReservaInventarioDto dto)
    {
        var reserva = await _service.LiberarAsync(id, dto);
        await AuditarAsync(AccionPermiso.Anular, reserva, "Reserva liberada.", dto.Motivo);
        return Ok(ApiResponse<ReservaInventarioDto>.Ok(reserva, "Reserva liberada correctamente."));
    }

    [HttpPost("{id:int}/expirar")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.CambiarEstado)]
    public Task<IActionResult> Expirar(int id) => EjecutarTransicionAsync(
        id, _service.ExpirarAsync, AccionPermiso.CambiarEstado, "Reserva expirada correctamente.");

    [HttpPost("{id:int}/cancelar")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Anular)]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarReservaInventarioDto dto)
    {
        var reserva = await _service.CancelarAsync(id, dto);
        await AuditarAsync(AccionPermiso.Anular, reserva, "Reserva cancelada.", dto.Motivo);
        return Ok(ApiResponse<ReservaInventarioDto>.Ok(reserva, "Reserva cancelada correctamente."));
    }

    private async Task<IActionResult> EjecutarTransicionAsync(
        int id,
        Func<int, Task<ReservaInventarioDto>> accion,
        AccionPermiso permiso,
        string mensaje)
    {
        var reserva = await accion(id);
        await AuditarAsync(permiso, reserva, mensaje);
        return Ok(ApiResponse<ReservaInventarioDto>.Ok(reserva, mensaje));
    }

    private Task AuditarAsync(
        AccionPermiso accion,
        ReservaInventarioDto reserva,
        string descripcion,
        string? motivo = null)
    {
        if (_auditoria is null) return Task.CompletedTask;
        return _auditoria.RegistrarAsync(
            ModuloSistema.MovimientosInventario,
            accion,
            descripcion,
            reserva.Id,
            entidad: nameof(ReservaInventario),
            valoresNuevos: new
            {
                reserva.Numero,
                reserva.VentaId,
                reserva.Estado,
                reserva.FechaExpiracion,
                Detalles = reserva.Detalles.Select(d => new
                {
                    d.ProductoVarianteId,
                    d.AlmacenId,
                    d.UbicacionAlmacenId,
                    d.CantidadReservada,
                    d.CantidadConsumida
                }).ToArray()
            },
            motivo: motivo);
    }
}
