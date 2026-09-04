using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs.Contabilidad;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("asientos-contables")]
public sealed class AsientosContablesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditoriaService _auditoria;

    public AsientosContablesController(AppDbContext db, IAuditoriaService auditoria)
    {
        _db = db;
        _auditoria = auditoria;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? numero,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 50,
        CancellationToken cancellationToken = default)
    {
        pagina = Math.Max(1, pagina);
        tamano = Math.Clamp(tamano, 1, 200);

        var query = _db.AsientosContables
            .AsNoTracking()
            .Include(a => a.Detalles)
                .ThenInclude(d => d.CuentaContable)
            .AsQueryable();

        if (desde.HasValue) query = query.Where(a => a.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(a => a.Fecha <= hasta.Value);
        if (!string.IsNullOrWhiteSpace(numero))
        {
            var normalized = numero.Trim();
            query = query.Where(a => a.Numero == normalized);
        }

        var total = await query.CountAsync(cancellationToken);
        var entities = await query
            .OrderByDescending(a => a.Fecha)
            .ThenByDescending(a => a.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new
        {
            pagina,
            tamano,
            total,
            items = entities.Select(AsientoContableApplicationService.Mapear).ToList()
        }));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var entity = await _db.AsientosContables
            .AsNoTracking()
            .Include(a => a.Detalles)
                .ThenInclude(d => d.CuentaContable)
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        return entity is null
            ? Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Asiento contable no encontrado",
                detail: $"No existe un asiento contable con Id {id}.")
            : Ok(ApiResponse<AsientoContableDto>.Ok(AsientoContableApplicationService.Mapear(entity)));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> Create(
        [FromBody] CrearAsientoContableDto dto,
        CancellationToken cancellationToken)
    {
        var entity = AsientoContableApplicationService.CrearAggregate(dto);

        if (!string.IsNullOrWhiteSpace(entity.Numero))
        {
            var replay = await _db.AsientosContables
                .AsNoTracking()
                .Include(a => a.Detalles)
                    .ThenInclude(d => d.CuentaContable)
                .SingleOrDefaultAsync(a => a.Numero == entity.Numero, cancellationToken);
            if (replay is not null)
                return Ok(ApiResponse<AsientoContableDto>.Ok(AsientoContableApplicationService.Mapear(replay)));
        }

        var cuentaIds = entity.Detalles.Select(d => d.CuentaContableId).Distinct().ToList();
        var cuentas = await _db.CuentasContables
            .Where(c => cuentaIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        foreach (var cuentaId in cuentaIds)
        {
            if (!cuentas.TryGetValue(cuentaId, out var cuenta))
                throw new ResourceNotFoundException($"La cuenta contable {cuentaId} no existe.");
            if (!cuenta.Activa)
                throw new BusinessRuleException($"La cuenta contable {cuenta.Codigo} está inactiva.");
            if (!cuenta.AceptaMovimientos)
                throw new BusinessRuleException($"La cuenta contable {cuenta.Codigo} no acepta movimientos directos.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        _db.AsientosContables.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Finanzas,
            AccionPermiso.Crear,
            $"Registró el asiento contable '{entity.Numero ?? entity.Id.ToString()}'.",
            entity.Id,
            entidad: "AsientoContable",
            valoresNuevos: new
            {
                entity.Fecha,
                entity.Concepto,
                entity.Numero,
                entity.DocumentoOrigenId,
                entity.TipoDocumentoOrigen,
                TotalDebe = entity.Detalles.Sum(d => d.Debe),
                TotalHaber = entity.Detalles.Sum(d => d.Haber),
                Detalles = entity.Detalles.Count
            });
        await transaction.CommitAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = entity.Id },
            ApiResponse<AsientoContableDto>.Ok(AsientoContableApplicationService.Mapear(entity)));
    }
}
