using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
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
    private readonly IProductoVarianteService _varianteService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageStorageService _imageStorageService;
    private readonly IAuditoriaService _auditoria;

    public ProductosController(
        IProductoService productoService,
        IProductoVarianteService varianteService,
        IUnitOfWork unitOfWork,
        IImageStorageService imageStorageService,
        IAuditoriaService auditoria)
    {
        _productoService = productoService;
        _varianteService = varianteService;
        _unitOfWork = unitOfWork;
        _imageStorageService = imageStorageService;
        _auditoria = auditoria;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetPaged([FromQuery] ProductoPagedRequest request)
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
        ProductoDto? creado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            creado = await _productoService.CreateAsync(dto);
            await SincronizarVariantesAsync(creado.Id, dto.Variantes, Array.Empty<ProductoVarianteDto>());
        });

        var resultado = await _productoService.GetByIdAsync(creado!.Id) ?? creado;
        return CreatedAtAction(nameof(GetById), new { id = resultado.Id },
            ApiResponse<ProductoDto>.Ok(resultado, "Producto y existencias por color creados correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateProductoDto dto)
    {
        ProductoDto? actualizado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var existentes = await _varianteService.GetByProductoIdAsync(id, incluirInactivas: true);
            actualizado = await _productoService.UpdateAsync(id, dto);
            if (actualizado is not null)
                await SincronizarVariantesAsync(id, dto.Variantes, existentes);
        });

        if (actualizado is null)
            return NotFound(ApiResponse<object>.Fail("Producto no encontrado."));

        var resultado = await _productoService.GetByIdAsync(id) ?? actualizado;
        return Ok(ApiResponse<ProductoDto>.Ok(resultado, "Producto, colores y existencias actualizados correctamente."));
    }

    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Activar)]
    public async Task<IActionResult> Activar(int id)
    {
        var producto = await _productoService.CambiarEstadoAsync(id, true);
        if (producto is null)
            return NotFound(ApiResponse<object>.Fail("Producto no encontrado."));

        return Ok(ApiResponse<ProductoDto>.Ok(producto, "Producto activado correctamente."));
    }

    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Desactivar)]
    public async Task<IActionResult> Desactivar(int id)
    {
        var producto = await _productoService.CambiarEstadoAsync(id, false);
        if (producto is null)
            return NotFound(ApiResponse<object>.Fail("Producto no encontrado."));

        return Ok(ApiResponse<ProductoDto>.Ok(producto, "Producto desactivado correctamente."));
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

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Exportar,
            $"Imagen descargada del producto: {producto.Nombre}.",
            imagenId,
            entidad: "ProductoImagen",
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
        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Exportar,
            $"Galería descargada del producto: {producto.Nombre}.",
            id,
            entidad: "Producto",
            valoresNuevos: new
            {
                productoId = id,
                imagenes = producto.Imagenes.Count,
                nombreArchivo = nombreZip
            });

        return File(memoryStream.ToArray(), "application/zip", nombreZip);
    }

    private async Task SincronizarVariantesAsync(
        int productoId,
        IReadOnlyCollection<ProductoVarianteFormularioDto> solicitadas,
        IReadOnlyCollection<ProductoVarianteDto> existentes)
    {
        if (solicitadas.Count == 0)
            return;

        if (solicitadas.Any(v => v.ColorId <= 0))
            throw new BusinessRuleException("Cada fila de existencias debe tener un color válido.");
        if (solicitadas.Any(v => v.Cantidad < 0))
            throw new BusinessRuleException("La cantidad por color no puede ser negativa.");
        if (solicitadas.Any(v => v.Costo <= 0 || v.Precio <= 0))
            throw new BusinessRuleException("Cada color debe tener costo y precio mayores que cero.");
        if (solicitadas.GroupBy(v => v.ColorId).Any(grupo => grupo.Count() > 1))
            throw new BusinessRuleException("No puedes registrar el mismo color más de una vez para el producto.");

        var skus = solicitadas
            .Where(v => !string.IsNullOrWhiteSpace(v.Sku))
            .Select(v => v.Sku!.Trim().ToUpperInvariant())
            .ToList();
        if (skus.Distinct(StringComparer.OrdinalIgnoreCase).Count() != skus.Count)
            throw new BusinessRuleException("No puedes repetir un SKU dentro del mismo producto.");

        var codigos = solicitadas
            .Where(v => !string.IsNullOrWhiteSpace(v.CodigoBarras))
            .Select(v => v.CodigoBarras!.Trim())
            .ToList();
        if (codigos.Distinct(StringComparer.OrdinalIgnoreCase).Count() != codigos.Count)
            throw new BusinessRuleException("No puedes repetir un código de barras dentro del mismo producto.");

        var idsSolicitados = solicitadas.Where(v => v.Id.HasValue).Select(v => v.Id!.Value).ToHashSet();
        foreach (var existente in existentes.Where(v => !idsSolicitados.Contains(v.Id)))
        {
            if (existente.Cantidad > 0)
                throw new BusinessRuleException($"No puedes retirar el color '{existente.ColorNombre}' porque todavía tiene {existente.Cantidad} unidades.");

            await _varianteService.DeleteAsync(productoId, existente.Id);
        }

        foreach (var solicitada in solicitadas)
        {
            var existente = solicitada.Id.HasValue
                ? existentes.FirstOrDefault(v => v.Id == solicitada.Id.Value)
                : null;

            if (solicitada.Id.HasValue && existente is null)
                throw new BusinessRuleException("Una de las variantes indicadas no pertenece al producto.");

            var sku = !string.IsNullOrWhiteSpace(solicitada.Sku)
                ? solicitada.Sku.Trim().ToUpperInvariant()
                : existente?.Sku ?? GenerarSku(productoId, solicitada.ColorId);

            var dto = new UpdateProductoVarianteDto
            {
                ColorId = solicitada.ColorId,
                Sku = sku,
                CodigoBarras = string.IsNullOrWhiteSpace(solicitada.CodigoBarras) ? null : solicitada.CodigoBarras.Trim(),
                Cantidad = solicitada.Cantidad,
                UmbralStockBajo = solicitada.UmbralStockBajo,
                Costo = solicitada.Costo,
                Precio = solicitada.Precio
            };

            if (existente is null)
            {
                var creada = await _varianteService.CreateAsync(productoId, dto);
                if (!solicitada.Activo)
                    await _varianteService.CambiarEstadoAsync(productoId, creada.Id, false);
            }
            else
            {
                var actualizada = await _varianteService.UpdateAsync(productoId, existente.Id, dto)
                    ?? throw new BusinessRuleException("No se pudo actualizar una de las variantes del producto.");
                if (actualizada.Activo != solicitada.Activo)
                    await _varianteService.CambiarEstadoAsync(productoId, existente.Id, solicitada.Activo);
            }
        }
    }

    private static string GenerarSku(int productoId, int colorId) =>
        $"VAR-{productoId:D6}-{colorId:D4}-{Guid.NewGuid():N}"[..31].ToUpperInvariant();
}
