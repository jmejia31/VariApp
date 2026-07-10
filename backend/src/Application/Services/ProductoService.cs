using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Mappings;
using InventoryApp.Application.Validators;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

public class ProductoService : IProductoService
{
    private const int MaxImagenes = 5;

    private readonly IProductoRepository _repository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IImageStorageService _imageStorage;
    private readonly ICurrentUserService _currentUser;

    public ProductoService(
        IProductoRepository repository,
        ICategoriaRepository categoriaRepository,
        IImageStorageService imageStorage,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _categoriaRepository = categoriaRepository;
        _imageStorage = imageStorage;
        _currentUser = currentUser;
    }

    public async Task<ProductoDto?> GetByIdAsync(int id)
    {
        var producto = await _repository.GetByIdAsync(id);
        return producto is null ? null : ProductoMapper.ToDto(producto);
    }

    public async Task<PagedResult<ProductoDto>> GetPagedAsync(PagedRequest request)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(request);
        return new PagedResult<ProductoDto>
        {
            Items = items.Select(ProductoMapper.ToDto).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductoDto> CreateAsync(CreateProductoDto dto)
    {
        var imagenes = dto.Imagenes ?? new List<Microsoft.AspNetCore.Http.IFormFile>();
        if (imagenes.Count > MaxImagenes)
            throw new BusinessRuleException($"Un producto puede tener máximo {MaxImagenes} fotos.");
        ValidarImagenes(imagenes);

        if (dto.CategoriaId.HasValue)
        {
            var categoria = await _categoriaRepository.GetByIdAsync(dto.CategoriaId.Value);
            if (categoria is null)
                throw new BusinessRuleException("La categoría seleccionada no existe.");
            if (!categoria.Activa)
                throw new BusinessRuleException("La categoría seleccionada está inactiva.");
        }

        var producto = new Producto
        {
            Nombre = dto.Nombre,
            Marca = dto.Marca,
            Modelo = dto.Modelo,
            Descripcion = dto.Descripcion,
            Cantidad = dto.Cantidad,
            Costo = dto.Costo,
            Precio = dto.Precio,
            UmbralStockBajo = dto.UmbralStockBajo,
            CategoriaId = dto.CategoriaId,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        for (int i = 0; i < imagenes.Count; i++)
        {
            var (url, publicId) = await _imageStorage.UploadAsync(imagenes[i]);
            producto.Imagenes.Add(new ProductoImagen
            {
                Url = url,
                PublicId = publicId,
                Orden = i,
                EsPrincipal = i == 0,
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            });
        }

        await _repository.AddAsync(producto);
        await _repository.SaveChangesAsync();

        return ProductoMapper.ToDto(producto);
    }

    public async Task<ProductoDto?> UpdateAsync(int id, UpdateProductoDto dto)
    {
        var producto = await _repository.GetByIdAsync(id);
        if (producto is null) return null;

        if (dto.CategoriaId.HasValue)
        {
            var categoria = await _categoriaRepository.GetByIdAsync(dto.CategoriaId.Value);
            if (categoria is null)
                throw new BusinessRuleException("La categoría seleccionada no existe.");
        }

        producto.Nombre = dto.Nombre;
        producto.Marca = dto.Marca;
        producto.Modelo = dto.Modelo;
        producto.Descripcion = dto.Descripcion;
        producto.Cantidad = dto.Cantidad;
        producto.Costo = dto.Costo;
        producto.Precio = dto.Precio;
        producto.UmbralStockBajo = dto.UmbralStockBajo;
        producto.CategoriaId = dto.CategoriaId;
        producto.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        producto.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        producto.FechaActualizacion = DateTime.UtcNow;

        // 1) Eliminar imágenes marcadas
        if (dto.ImagenesAEliminarIds is { Count: > 0 })
        {
            var aEliminar = producto.Imagenes.Where(i => dto.ImagenesAEliminarIds.Contains(i.Id)).ToList();
            foreach (var img in aEliminar)
            {
                await _imageStorage.DeleteAsync(img.PublicId);
                producto.Imagenes.Remove(img);
            }
        }

        // 2) Validar límite antes de agregar nuevas
        var nuevas = dto.ImagenesNuevas ?? new List<Microsoft.AspNetCore.Http.IFormFile>();
        if (producto.Imagenes.Count + nuevas.Count > MaxImagenes)
            throw new BusinessRuleException(
                $"Un producto puede tener máximo {MaxImagenes} fotos ({producto.Imagenes.Count} existentes + {nuevas.Count} nuevas excede el límite).");
        ValidarImagenes(nuevas);

        // 3) Agregar nuevas imágenes
        var siguienteOrden = producto.Imagenes.Count == 0 ? 0 : producto.Imagenes.Max(i => i.Orden) + 1;
        var yaTienePrincipal = producto.Imagenes.Any(i => i.EsPrincipal);

        foreach (var archivo in nuevas)
        {
            var (url, publicId) = await _imageStorage.UploadAsync(archivo);
            producto.Imagenes.Add(new ProductoImagen
            {
                Url = url,
                PublicId = publicId,
                Orden = siguienteOrden++,
                EsPrincipal = !yaTienePrincipal && producto.Imagenes.Count == 0,
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            });
            yaTienePrincipal = true;
        }

        // 4) Cambiar imagen principal si se solicitó
        if (dto.ImagenPrincipalId.HasValue)
        {
            var nuevaPrincipal = producto.Imagenes.FirstOrDefault(i => i.Id == dto.ImagenPrincipalId.Value);
            if (nuevaPrincipal is null)
                throw new BusinessRuleException("La imagen indicada como principal no pertenece a este producto.");

            foreach (var img in producto.Imagenes) img.EsPrincipal = false;
            nuevaPrincipal.EsPrincipal = true;
        }
        else if (producto.Imagenes.Count > 0 && !producto.Imagenes.Any(i => i.EsPrincipal))
        {
            // Garantizar que siempre haya una principal si quedan imágenes
            producto.Imagenes.OrderBy(i => i.Orden).First().EsPrincipal = true;
        }

        _repository.Update(producto);
        await _repository.SaveChangesAsync();

        return ProductoMapper.ToDto(producto);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var producto = await _repository.GetByIdAsync(id);
        if (producto is null) return false;

        foreach (var imagen in producto.Imagenes)
            await _imageStorage.DeleteAsync(imagen.PublicId);

        _repository.Remove(producto);
        return await _repository.SaveChangesAsync();
    }

    private static void ValidarImagenes(IEnumerable<Microsoft.AspNetCore.Http.IFormFile> imagenes)
    {
        if (imagenes.Any(imagen => !ImagenValidationHelper.EsImagenValida(imagen)))
        {
            throw new BusinessRuleException("Solo se permiten imagenes JPG, PNG o WebP de hasta 5 MB.");
        }
    }

}
