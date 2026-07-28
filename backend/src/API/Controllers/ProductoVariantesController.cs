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
[Route("productos/{productoId:int}/variantes")]
public class ProductoVariantesController : ControllerBase
{
    private readonly IProductoVarianteService _service;

    public ProductoVariantesController(IProductoVarianteService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll(int productoId, [FromQuery] bool incluirInactivas = true) =>
        Ok(ApiResponse<List<ProductoVarianteDto>>.Ok(await _service.GetByProductoIdAsync(productoId, incluirInactivas)));

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int productoId, int id)
    {
        var variante = await _service.GetByIdAsync(productoId, id);
        return variante is null
            ? NotFound(ApiResponse<object>.Fail("Variante no encontrada."))
            : Ok(ApiResponse<ProductoVarianteDto>.Ok(variante));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Crear)]
    public async Task<IActionResult> Create(int productoId, CreateProductoVarianteDto dto)
    {
        var creada = await _service.CreateAsync(productoId, dto);
        return CreatedAtAction(nameof(GetById), new { productoId, id = creada.Id },
            ApiResponse<ProductoVarianteDto>.Ok(creada, "Variante creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int productoId, int id, UpdateProductoVarianteDto dto)
    {
        var actualizada = await _service.UpdateAsync(productoId, id, dto);
        return actualizada is null
            ? NotFound(ApiResponse<object>.Fail("Variante no encontrada."))
            : Ok(ApiResponse<ProductoVarianteDto>.Ok(actualizada, "Variante actualizada correctamente."));
    }

    [HttpPatch("{id:int}/estado")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Editar)]
    public async Task<IActionResult> CambiarEstado(int productoId, int id, CambiarEstadoProductoVarianteDto dto)
    {
        var actualizada = await _service.CambiarEstadoAsync(productoId, id, dto.Activo);
        return actualizada is null
            ? NotFound(ApiResponse<object>.Fail("Variante no encontrada."))
            : Ok(ApiResponse<ProductoVarianteDto>.Ok(actualizada, dto.Activo ? "Variante activada." : "Variante desactivada."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> Delete(int productoId, int id)
    {
        var eliminada = await _service.DeleteAsync(productoId, id);
        return eliminada
            ? Ok(ApiResponse<object>.Ok(new { }, "Variante eliminada lógicamente."))
            : NotFound(ApiResponse<object>.Fail("Variante no encontrada."));
    }
}
