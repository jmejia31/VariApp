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
[Route("productos")]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _productoService;

    public ProductosController(IProductoService productoService)
    {
        _productoService = productoService;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetPaged([FromQuery] PagedRequest request)
    {
        var resultado = await _productoService.GetPagedAsync(request);
        return Ok(ApiResponse<PagedResult<ProductoDto>>.Ok(resultado));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var producto = await _productoService.GetByIdAsync(id);
        if (producto is null)
            return NotFound(ApiResponse<object>.Fail("Producto no encontrado."));

        return Ok(ApiResponse<ProductoDto>.Ok(producto));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromForm] CreateProductoDto dto)
    {
        var creado = await _productoService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id },
            ApiResponse<ProductoDto>.Ok(creado, "Producto creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateProductoDto dto)
    {
        var actualizado = await _productoService.UpdateAsync(id, dto);
        if (actualizado is null)
            return NotFound(ApiResponse<object>.Fail("Producto no encontrado."));

        return Ok(ApiResponse<ProductoDto>.Ok(actualizado, "Producto actualizado correctamente."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Eliminar)]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _productoService.DeleteAsync(id);
        if (!eliminado)
            return NotFound(ApiResponse<object>.Fail("Producto no encontrado."));

        return Ok(ApiResponse<object>.Ok(new { }, "Producto eliminado correctamente."));
    }
}
