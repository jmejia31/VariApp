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
    private readonly ICategoriaService _categoriaService;

    public TiendaController(IProductoService productoService, ICategoriaService categoriaService)
    {
        _productoService = productoService;
        _categoriaService = categoriaService;
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

    [HttpGet("productos/{slug}")]
    public async Task<IActionResult> GetProducto(string slug)
    {
        if (!PublicSlug.TryGetId(slug, out var id))
            return NotFound(ApiResponse<ProductoCatalogoPublicoDto>.Fail("Producto no encontrado."));

        var producto = await _productoService.GetByIdAsync(id);
        if (producto is null || !producto.Activo)
            return NotFound(ApiResponse<ProductoCatalogoPublicoDto>.Fail("Producto no encontrado."));

        return Ok(ApiResponse<ProductoCatalogoPublicoDto>.Ok(MapearProducto(producto)));
    }

    [HttpGet("categorias")]
    public async Task<IActionResult> GetCategorias()
    {
        var categorias = await _categoriaService.GetActivasAsync();
        var resultado = categorias
            .Where(categoria => categoria.Activa)
            .OrderBy(categoria => categoria.Nombre)
            .Select(MapearCategoria)
            .ToList();

        return Ok(ApiResponse<List<CategoriaCatalogoPublicoDto>>.Ok(resultado));
    }

    [HttpGet("categorias/{slug}")]
    public async Task<IActionResult> GetCategoria(string slug)
    {
        if (!PublicSlug.TryGetId(slug, out var id))
            return NotFound(ApiResponse<CategoriaCatalogoPublicoDto>.Fail("Categoria no encontrada."));

        var categoria = await _categoriaService.GetByIdAsync(id);
        if (categoria is null || !categoria.Activa)
            return NotFound(ApiResponse<CategoriaCatalogoPublicoDto>.Fail("Categoria no encontrada."));

        return Ok(ApiResponse<CategoriaCatalogoPublicoDto>.Ok(MapearCategoria(categoria)));
    }

    private static CategoriaCatalogoPublicoDto MapearCategoria(CategoriaDto categoria) => new()
    {
        Id = categoria.Id,
        Slug = PublicSlug.Create(categoria.Nombre, categoria.Id),
        Nombre = categoria.Nombre,
        Descripcion = categoria.Descripcion,
        TotalProductos = null
    };

    private static ProductoCatalogoPublicoDto MapearProducto(ProductoDto producto)
    {
        var variantesActivas = producto.Variantes.Where(v => v.Activo).ToList();
        var skusProducto = variantesActivas.Select(v => v.Sku).Where(sku => !string.IsNullOrWhiteSpace(sku)).Distinct().ToList();
        var cantidadPublica = variantesActivas.Count > 0
            ? variantesActivas.Sum(v => Math.Max(0, v.Cantidad))
            : Math.Max(0, producto.Cantidad);
        var preciosVariantes = variantesActivas.Where(v => v.Precio > 0).Select(v => v.Precio).ToList();
        var precioPublico = preciosVariantes.Count > 0
            ? preciosVariantes.Min()
            : producto.PrecioMinimo > 0 ? producto.PrecioMinimo : producto.Precio;

        return new ProductoCatalogoPublicoDto
        {
            Id = producto.Id,
            Slug = PublicSlug.Create(producto.Nombre, producto.Id),
            Nombre = producto.Nombre,
            Descripcion = producto.Descripcion,
            CategoriaId = producto.CategoriaId,
            CategoriaNombre = producto.CategoriaNombre,
            MarcaNombre = producto.MarcaNombre ?? producto.Marca,
            ModeloNombre = producto.ModeloNombre ?? producto.Modelo,
            Precio = Math.Max(0, precioPublico),
            PrecioOferta = null,
            CantidadDisponible = cantidadPublica,
            EstaAgotado = cantidadPublica <= 0,
            Sku = skusProducto.Count == 1 ? skusProducto[0] : null,
            Activo = producto.Activo,
            EsDestacado = false,
            FechaCreacion = producto.FechaCreacion,
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
            Modelos = variantesActivas
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
                        .Select(grupo => grupo.First())
                        .ToList();
                    var skus = g.Select(v => v.Sku).Where(sku => !string.IsNullOrWhiteSpace(sku)).Distinct().ToList();
                    var cantidad = g.Sum(v => Math.Max(0, v.Cantidad));

                    return new ModeloCatalogoPublicoDto
                    {
                        ModeloId = g.Key.ModeloId,
                        ModeloNombre = g.Key.ModeloNombre,
                        MarcaNombre = g.Key.MarcaNombre,
                        Sku = skus.Count == 1 ? skus[0] : null,
                        Precio = g.Where(v => v.Precio > 0).Select(v => v.Precio).DefaultIfEmpty().Min(),
                        CantidadDisponible = cantidad,
                        EstaAgotado = cantidad <= 0,
                        Imagenes = imagenes
                    };
                })
                .ToList()
        };
    }
}
