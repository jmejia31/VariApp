using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace InventoryApp.Application.Services;

public sealed class ProductoVarianteImagenService : IProductoVarianteImagenService
{
    private const int MaximoImagenesPorVariante = 5;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _varianteRepository;
    private readonly IImageStorageService _storage;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;

    public ProductoVarianteImagenService(
        IProductoRepository productoRepository,
        IProductoVarianteRepository varianteRepository,
        IImageStorageService storage,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria)
    {
        _productoRepository = productoRepository;
        _varianteRepository = varianteRepository;
        _storage = storage;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    public async Task<IReadOnlyList<ProductoImagenDto>?> GetAsync(int productoId, int varianteId)
    {
        var producto = await _productoRepository.GetByIdAsync(productoId);
        var variante = await _varianteRepository.GetByIdAsync(varianteId);
        if (producto is null || variante is null || variante.ProductoId != productoId) return null;

        var especificas = producto.Imagenes.Where(x => x.ProductoVarianteId == varianteId).ToList();
        if (especificas.Count > 0) return Map(especificas);

        // Fallback explícito: si la variante no tiene galería propia se usan
        // únicamente las imágenes generales del producto. El DTO conserva
        // ProductoVarianteId = null para que el frontend pueda mostrar que es fallback.
        return Map(producto.Imagenes.Where(x => x.ProductoVarianteId == null));
    }

    public async Task<IReadOnlyList<ProductoImagenDto>> AddAsync(
        int productoId,
        int varianteId,
        IReadOnlyCollection<IFormFile> archivos)
    {
        if (archivos.Count == 0)
            throw new BusinessRuleException("Selecciona al menos una imagen para la variante.");

        var producto = await _productoRepository.GetByIdAsync(productoId)
            ?? throw new BusinessRuleException("Producto no encontrado.");
        var variante = await _varianteRepository.GetByIdAsync(varianteId);
        if (variante is null || variante.ProductoId != productoId)
            throw new BusinessRuleException("La variante no existe o no pertenece al producto.");

        var actuales = producto.Imagenes.Where(x => x.ProductoVarianteId == varianteId).ToList();
        if (actuales.Count + archivos.Count > MaximoImagenesPorVariante)
            throw new BusinessRuleException($"Cada variante puede tener hasta {MaximoImagenesPorVariante} imágenes.");

        var subidas = new List<ProductoImagen>();
        try
        {
            var orden = actuales.Count == 0 ? 0 : actuales.Max(x => x.Orden) + 1;
            foreach (var archivo in archivos)
            {
                var (url, publicId) = await _storage.UploadAsync(archivo);
                var imagen = new ProductoImagen
                {
                    ProductoId = productoId,
                    ProductoVarianteId = varianteId,
                    Url = url,
                    PublicId = publicId,
                    Orden = orden++,
                    EsPrincipal = actuales.Count == 0 && subidas.Count == 0,
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                };
                producto.Imagenes.Add(imagen);
                subidas.Add(imagen);
            }
            await _productoRepository.SaveChangesAsync();
        }
        catch
        {
            foreach (var imagen in subidas)
            {
                try { await _storage.DeleteAsync(imagen.PublicId); } catch { }
            }
            throw;
        }

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Editar,
            $"Se agregaron {subidas.Count} imagen(es) a una variante exacta.",
            varianteId,
            entidad: "ProductoVarianteImagen",
            valoresNuevos: new { productoId, varianteId, imagenes = subidas.Select(x => x.Id).ToArray() });

        return Map(producto.Imagenes.Where(x => x.ProductoVarianteId == varianteId));
    }

    public async Task<bool> SetPrincipalAsync(int productoId, int varianteId, int imagenId)
    {
        var producto = await _productoRepository.GetByIdAsync(productoId);
        var variante = await _varianteRepository.GetByIdAsync(varianteId);
        if (producto is null || variante is null || variante.ProductoId != productoId) return false;

        var imagenes = producto.Imagenes.Where(x => x.ProductoVarianteId == varianteId).ToList();
        var seleccionada = imagenes.FirstOrDefault(x => x.Id == imagenId);
        if (seleccionada is null) return false;
        foreach (var imagen in imagenes) imagen.EsPrincipal = imagen.Id == imagenId;
        await _productoRepository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Editar,
            "Se cambió la imagen principal de una variante.",
            imagenId,
            entidad: "ProductoVarianteImagen",
            valoresNuevos: new { productoId, varianteId, imagenId });
        return true;
    }

    public async Task<bool> DeleteAsync(int productoId, int varianteId, int imagenId)
    {
        var producto = await _productoRepository.GetByIdAsync(productoId);
        var variante = await _varianteRepository.GetByIdAsync(varianteId);
        if (producto is null || variante is null || variante.ProductoId != productoId) return false;

        var imagenes = producto.Imagenes.Where(x => x.ProductoVarianteId == varianteId).OrderBy(x => x.Orden).ToList();
        var imagen = imagenes.FirstOrDefault(x => x.Id == imagenId);
        if (imagen is null) return false;
        var eraPrincipal = imagen.EsPrincipal;
        producto.Imagenes.Remove(imagen);
        if (eraPrincipal)
        {
            var siguiente = imagenes.FirstOrDefault(x => x.Id != imagenId);
            if (siguiente is not null) siguiente.EsPrincipal = true;
        }
        await _productoRepository.SaveChangesAsync();

        try { await _storage.DeleteAsync(imagen.PublicId); } catch { }
        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Editar,
            "Se eliminó una imagen de una variante exacta.",
            imagenId,
            entidad: "ProductoVarianteImagen",
            valoresNuevos: new { productoId, varianteId, imagenId });
        return true;
    }

    private static IReadOnlyList<ProductoImagenDto> Map(IEnumerable<ProductoImagen> imagenes) =>
        imagenes.OrderBy(x => x.Orden).Select(x => new ProductoImagenDto
        {
            Id = x.Id,
            Url = x.Url,
            Orden = x.Orden,
            EsPrincipal = x.EsPrincipal,
            ProductoVarianteId = x.ProductoVarianteId
        }).ToList();
}
