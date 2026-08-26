using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
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
[Route("notas-credito-cliente")]
public sealed class NotasCreditoClienteController : ControllerBase
{
    private readonly INotaCreditoClienteService _service;

    public NotasCreditoClienteController(
        AppDbContext db,
        IFacturaRepository facturas,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria)
    {
        ArgumentNullException.ThrowIfNull(db);
        _service = new NotaCreditoClienteService(
            new NotaCreditoClienteRepository(db),
            facturas,
            currentUser,
            unitOfWork,
            auditoria);
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var nota = await _service.GetByIdAsync(id);
        return nota is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Nota de crédito de cliente no encontrada",
                detail: "No existe una nota de crédito de cliente con el identificador indicado.")
            : Ok(ApiResponse<NotaCreditoClienteDto>.Ok(nota));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateNotaCreditoClienteDto dto)
    {
        var creada = await _service.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = creada.Id },
            ApiResponse<NotaCreditoClienteDto>.Ok(creada, "Nota de crédito de cliente creada correctamente."));
    }
}
