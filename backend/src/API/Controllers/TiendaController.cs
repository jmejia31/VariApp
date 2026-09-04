using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("tienda")]
public sealed class TiendaController : ControllerBase
{
    private readonly IProductoService _productoService;

    public TiendaController(IProductoService productoService)
    {
        _productoService = productoService;
    }

    [HttpGet("productos")]
    public async Task<IActionResult> GetProductos([FromQuery] ProductoPagedRequest request)
    {
        request.Activo = true;
        request.UsuarioIdScope = null;

        var resultado = await _productoService.GetPagedAsync(request);
        var catalogo = new PagedResult<ProductoCatalogoPublicoDto>
        {
            Items = resultado.Items
                .Where(producto => producto.Activo)
                .Select(MapearProducto)
                .ToList(),
            Page = resultado.Page,
            PageSize = resultado.PageSize,
            TotalCount = resultado.TotalCount
        };

        return Ok(ApiResponse<PagedResult<ProductoCatalogoPublicoDto>>.Ok(catalogo));
    }

    private static ProductoCatalogoPublicoDto MapearProducto(ProductoDto producto) => new()
    {
        Id = producto.Id,
        Nombre = producto.Nombre,
        Descripcion = producto.Descripcion,
        CategoriaNombre = producto.CategoriaNombre,
        MarcaNombre = producto.MarcaNombre ?? producto.Marca,
        ModeloNombre = producto.ModeloNombre ?? producto.Modelo,
        Precio = producto.PrecioMinimo > 0 ? producto.PrecioMinimo : producto.Precio,
        CantidadDisponible = producto.Cantidad,
        EstaAgotado = producto.EstaAgotado || producto.Cantidad <= 0,
        ImagenPrincipalUrl = producto.ImagenPrincipalUrl,
        Imagenes = producto.Imagenes
            .OrderBy(imagen => imagen.Orden)
            .Select(imagen => new ProductoImagenPublicaDto
            {
                Url = imagen.Url,
                Orden = imagen.Orden,
                EsPrincipal = imagen.EsPrincipal
            })
            .ToList(),
        Modelos = producto.Variantes
            .Where(v => v.Activo)
            .GroupBy(v => new { v.ModeloId, v.ModeloNombre, v.MarcaNombre })
            .OrderBy(g => g.Key.ModeloNombre)
            .Select(g =>
            {
                var imagenesEspecificas = g.SelectMany(v => v.Imagenes)
                    .Where(i => !string.IsNullOrWhiteSpace(i.Url))
                    .OrderBy(i => i.Orden)
                    .Select(i => new ProductoImagenPublicaDto { Url = i.Url, Orden = i.Orden, EsPrincipal = i.EsPrincipal })
                    .ToList();
                var imagenes = (imagenesEspecificas.Count > 0 ? imagenesEspecificas : producto.Imagenes
                        .OrderBy(i => i.Orden)
                        .Select(i => new ProductoImagenPublicaDto { Url = i.Url, Orden = i.Orden, EsPrincipal = i.EsPrincipal }))
                    .GroupBy(i => i.Url)
                    .Select(g => g.First())
                    .ToList();

                return new ModeloCatalogoPublicoDto
                {
                    ModeloId = g.Key.ModeloId,
                    ModeloNombre = g.Key.ModeloNombre,
                    MarcaNombre = g.Key.MarcaNombre,
                    Precio = g.Where(v => v.Precio > 0).Select(v => v.Precio).DefaultIfEmpty().Min(),
                    CantidadDisponible = g.Sum(v => v.Cantidad),
                    EstaAgotado = g.All(v => v.Cantidad <= 0),
                    Imagenes = imagenes
                };
            })
            .ToList()
    };
}
