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
    private const int MaxLineasCheckout = 50;
    private const int MaxUnidadesPorLinea = 999;
    private const int MaxLongitudIdentidadVariante = 200;
    private static readonly TimeSpan VigenciaValidacionCheckout = TimeSpan.FromMinutes(10);

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

    /// <summary>
    /// Recalcula precio, variante y stock exclusivamente desde el catálogo vigente.
    /// El cliente nunca envía importes y esta validación no reserva inventario ni crea un pedido ERP.
    /// </summary>
    [HttpPost("checkout/validar")]
    public async Task<IActionResult> ValidarCheckout([FromBody] ValidarCheckoutTiendaDto dto)
    {
        if (dto?.Items is null || dto.Items.Count == 0)
            return BadRequest(ApiResponse<CheckoutTiendaValidadoDto>.Fail("El carrito está vacío."));

        if (dto.Items.Count > MaxLineasCheckout)
            return BadRequest(ApiResponse<CheckoutTiendaValidadoDto>.Fail("El carrito supera el máximo de líneas permitido."));

        if (dto.Items.Any(item =>
                item.ProductoId <= 0
                || item.Unidades <= 0
                || item.Unidades > MaxUnidadesPorLinea
                || (item.ModeloNombre?.Length ?? 0) > MaxLongitudIdentidadVariante
                || (item.MarcaNombre?.Length ?? 0) > MaxLongitudIdentidadVariante))
        {
            return BadRequest(ApiResponse<CheckoutTiendaValidadoDto>.Fail("El carrito contiene una referencia o cantidad no válida."));
        }

        var agrupadas = dto.Items
            .GroupBy(item => new
            {
                item.ProductoId,
                item.ModeloId,
                ModeloNombre = string.IsNullOrEmpty(item.ModeloNombre) ? null : item.ModeloNombre,
                MarcaNombre = string.IsNullOrEmpty(item.MarcaNombre) ? null : item.MarcaNombre
            })
            .Select(grupo => new CheckoutTiendaItemRequestDto
            {
                ProductoId = grupo.Key.ProductoId,
                ModeloId = grupo.Key.ModeloId,
                ModeloNombre = grupo.Key.ModeloNombre,
                MarcaNombre = grupo.Key.MarcaNombre,
                Unidades = grupo.Sum(item => item.Unidades)
            })
            .ToList();

        if (agrupadas.Any(item => item.Unidades > MaxUnidadesPorLinea))
            return BadRequest(ApiResponse<CheckoutTiendaValidadoDto>.Fail("La cantidad acumulada de un producto supera el máximo permitido."));

        var lineas = new List<CheckoutTiendaLineaDto>(agrupadas.Count);

        foreach (var solicitud in agrupadas)
        {
            var producto = await _productoService.GetByIdAsync(solicitud.ProductoId);
            if (producto is null || !producto.Activo)
                return Conflict(ApiResponse<CheckoutTiendaValidadoDto>.Fail("Uno de los productos ya no está disponible. Actualiza el carrito antes de continuar."));

            var variantesActivas = producto.Variantes.Where(variante => variante.Activo).ToList();
            int stock;
            decimal precio;
            string? modelo;
            string? sku;

            if (variantesActivas.Count > 0)
            {
                var variantesModelo = variantesActivas
                    .Where(variante =>
                        variante.ModeloId == solicitud.ModeloId
                        && string.Equals(variante.ModeloNombre ?? string.Empty, solicitud.ModeloNombre ?? string.Empty, StringComparison.Ordinal)
                        && string.Equals(variante.MarcaNombre ?? string.Empty, solicitud.MarcaNombre ?? string.Empty, StringComparison.Ordinal))
                    .ToList();

                if (variantesModelo.Count == 0)
                    return Conflict(ApiResponse<CheckoutTiendaValidadoDto>.Fail("Una variante seleccionada ya no está disponible. Actualiza el carrito antes de continuar."));

                stock = variantesModelo.Sum(variante => Math.Max(0, variante.Cantidad));
                precio = variantesModelo
                    .Where(variante => variante.Precio > 0)
                    .Select(variante => variante.Precio)
                    .DefaultIfEmpty(0)
                    .Min();
                modelo = variantesModelo.Select(variante => variante.ModeloNombre).FirstOrDefault(nombre => !string.IsNullOrWhiteSpace(nombre));
                var skus = variantesModelo.Select(variante => variante.Sku).Where(valor => !string.IsNullOrWhiteSpace(valor)).Distinct().ToList();
                sku = skus.Count == 1 ? skus[0] : null;
            }
            else
            {
                if (solicitud.ModeloId is not null || solicitud.ModeloNombre is not null || solicitud.MarcaNombre is not null)
                    return Conflict(ApiResponse<CheckoutTiendaValidadoDto>.Fail("La variante seleccionada ya no existe. Actualiza el carrito antes de continuar."));

                stock = Math.Max(0, producto.Cantidad);
                precio = producto.PrecioMinimo > 0 ? producto.PrecioMinimo : producto.Precio;
                modelo = producto.ModeloNombre ?? producto.Modelo;
                sku = null;
            }

            precio = Math.Max(0, precio);
            if (precio <= 0)
                return Conflict(ApiResponse<CheckoutTiendaValidadoDto>.Fail("Uno de los productos no tiene un precio público válido. Intenta nuevamente más tarde."));

            if (stock < solicitud.Unidades)
                return Conflict(ApiResponse<CheckoutTiendaValidadoDto>.Fail("Cambió la existencia disponible de uno de los productos. Actualiza el carrito antes de continuar."));

            lineas.Add(new CheckoutTiendaLineaDto
            {
                ProductoId = producto.Id,
                ModeloId = solicitud.ModeloId,
                Nombre = producto.Nombre,
                Modelo = modelo,
                Sku = sku,
                Unidades = solicitud.Unidades,
                StockDisponible = stock,
                PrecioUnitario = precio,
                Total = precio * solicitud.Unidades
            });
        }

        var subtotal = lineas.Sum(linea => linea.Total);
        var validado = new CheckoutTiendaValidadoDto
        {
            ValidacionId = Guid.NewGuid().ToString("N"),
            ExpiraUtc = DateTime.UtcNow.Add(VigenciaValidacionCheckout),
            Subtotal = subtotal,
            Total = subtotal,
            Lineas = lineas
        };

        return Ok(ApiResponse<CheckoutTiendaValidadoDto>.Ok(validado, "Carrito validado contra el catálogo vigente."));
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
