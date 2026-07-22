using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("productos")]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _productoService;
    private readonly IImageStorageService _imageStorageService;
    private readonly IAuditoriaService _auditoria;

    public ProductosController(IProductoService productoService, IImageStorageService imageStorageService, IAuditoriaService auditoria)
    {
        _productoService = productoService;
        _imageStorageService = imageStorageService;
        _auditoria = auditoria;
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
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _productoService.DeleteAsync(id);
        if (!eliminado)
            return NotFound(ApiResponse<object>.Fail("Producto no encontrado."));

        return Ok(ApiResponse<object>.Ok(new { }, "Producto eliminado lógicamente. Su historial permanece protegido."));
    }

    [HttpGet("{id:int}/imagenes/{imagenId:int}/descargar")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Exportar)]
    public async Task<IActionResult> DescargarImagen(int id, int imagenId)
    {
        var producto = await _productoService.GetByIdAsync(id);
        if (producto is null)
            return NotFound(ApiResponse<object>.Fail("Producto no encontrado."));

        var imagen = producto.Imagenes.FirstOrDefault(i => i.Id == imagenId);
        if (imagen is null)
            return NotFound(ApiResponse<object>.Fail("La imagen no existe o no pertenece a este producto."));

        var descarga = await _imageStorageService.DownloadAsync(imagen.Url);
        if (descarga is null)
            return NotFound(ApiResponse<object>.Fail("El archivo de la imagen ya no está disponible."));

        var (contenido, contentType) = descarga.Value;
        var extension = contentType.Contains("png") ? "png" : contentType.Contains("webp") ? "webp" : "jpg";
        var nombreArchivo = $"{producto.Nombre}-{imagen.Orden + 1}.{extension}".Replace(" ", "_");

        await _auditoria.RegistrarAsync(ModuloSistema.Productos, AccionPermiso.Exportar,
            $"Imagen descargada del producto: {producto.Nombre}.", imagenId, entidad: "ProductoImagen",
            valoresNuevos: new { productoId = id, imagenId, nombreArchivo });

        return File(contenido, contentType, nombreArchivo);
    }

    [HttpGet("{id:int}/imagenes/descargar-todas")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Exportar)]
    public async Task<IActionResult> DescargarTodasLasImagenes(int id)
    {
        var producto = await _productoService.GetByIdAsync(id);
        if (producto is null)
            return NotFound(ApiResponse<object>.Fail("Producto no encontrado."));

        if (producto.Imagenes.Count == 0)
            return NotFound(ApiResponse<object>.Fail("Este producto no tiene imágenes."));

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var contador = 1;
            foreach (var imagen in producto.Imagenes.OrderBy(i => i.Orden))
            {
                var descarga = await _imageStorageService.DownloadAsync(imagen.Url);
                if (descarga is null) continue;

                var (contenido, contentType) = descarga.Value;
                var extension = contentType.Contains("png") ? "png" : contentType.Contains("webp") ? "webp" : "jpg";
                var entry = archive.CreateEntry($"{contador}.{extension}", CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                await contenido.CopyToAsync(entryStream);
                contador++;
            }
        }

        memoryStream.Position = 0;
        var nombreZip = $"{producto.Nombre}-imagenes.zip".Replace(" ", "_");
        await _auditoria.RegistrarAsync(ModuloSistema.Productos, AccionPermiso.Exportar,
            $"Galería descargada del producto: {producto.Nombre}.", id, entidad: "Producto",
            valoresNuevos: new { productoId = id, imagenes = producto.Imagenes.Count, nombreArchivo = nombreZip });

        return File(memoryStream.ToArray(), "application/zip", nombreZip);
    }
}
