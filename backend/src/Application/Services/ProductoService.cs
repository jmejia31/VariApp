using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

public class ProductoService : IProductoService
{
    private readonly IProductoRepository _repository;
    private readonly IImageStorageService _imageStorage;

    public ProductoService(IProductoRepository repository, IImageStorageService imageStorage)
    {
        _repository = repository;
        _imageStorage = imageStorage;
    }

    public async Task<ProductoDto?> GetByIdAsync(int id)
    {
        var producto = await _repository.GetByIdAsync(id);
        return producto is null ? null : ToDto(producto);
    }

    public async Task<PagedResult<ProductoDto>> GetPagedAsync(PagedRequest request)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(request);
        return new PagedResult<ProductoDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductoDto> CreateAsync(CreateProductoDto dto)
    {
        var producto = CreateEntity(dto);

        if (dto.Imagen is not null)
        {
            var (url, publicId) = await _imageStorage.UploadAsync(dto.Imagen);
            producto.ImagenUrl = url;
            producto.ImagenPublicId = publicId;
        }

        await _repository.AddAsync(producto);
        await _repository.SaveChangesAsync();

        return ToDto(producto);
    }

    public async Task<ProductoDto?> UpdateAsync(int id, UpdateProductoDto dto)
    {
        var producto = await _repository.GetByIdAsync(id);
        if (producto is null) return null;

        // Guardamos referencia de la imagen anterior por si hay que borrarla
        var imagenAnteriorPublicId = producto.ImagenPublicId;

        ApplyUpdate(dto, producto);

        if (dto.Imagen is not null)
        {
            var (url, publicId) = await _imageStorage.UploadAsync(dto.Imagen);
            producto.ImagenUrl = url;
            producto.ImagenPublicId = publicId;

            if (!string.IsNullOrEmpty(imagenAnteriorPublicId))
                await _imageStorage.DeleteAsync(imagenAnteriorPublicId);
        }
        else if (dto.EliminarImagen && !string.IsNullOrEmpty(imagenAnteriorPublicId))
        {
            await _imageStorage.DeleteAsync(imagenAnteriorPublicId);
            producto.ImagenUrl = null;
            producto.ImagenPublicId = null;
        }

        _repository.Update(producto);
        await _repository.SaveChangesAsync();

        return ToDto(producto);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var producto = await _repository.GetByIdAsync(id);
        if (producto is null) return false;

        if (!string.IsNullOrEmpty(producto.ImagenPublicId))
            await _imageStorage.DeleteAsync(producto.ImagenPublicId);

        _repository.Remove(producto);
        return await _repository.SaveChangesAsync();
    }

    private static Producto CreateEntity(CreateProductoDto dto) => new()
    {
        Nombre = dto.Nombre,
        Marca = dto.Marca,
        Modelo = dto.Modelo,
        Descripcion = dto.Descripcion,
        Cantidad = dto.Cantidad,
        Costo = dto.Costo,
        Precio = dto.Precio,
        UmbralStockBajo = dto.UmbralStockBajo
    };

    private static void ApplyUpdate(UpdateProductoDto dto, Producto producto)
    {
        producto.Nombre = dto.Nombre;
        producto.Marca = dto.Marca;
        producto.Modelo = dto.Modelo;
        producto.Descripcion = dto.Descripcion;
        producto.Cantidad = dto.Cantidad;
        producto.Costo = dto.Costo;
        producto.Precio = dto.Precio;
        producto.UmbralStockBajo = dto.UmbralStockBajo;
        producto.FechaActualizacion = DateTime.UtcNow;
    }

    private static ProductoDto ToDto(Producto producto) => new()
    {
        Id = producto.Id,
        Nombre = producto.Nombre,
        Marca = producto.Marca,
        Modelo = producto.Modelo,
        Descripcion = producto.Descripcion,
        Cantidad = producto.Cantidad,
        Costo = producto.Costo,
        Precio = producto.Precio,
        ImagenUrl = producto.ImagenUrl,
        UmbralStockBajo = producto.UmbralStockBajo,
        TieneStockBajo = producto.TieneStockBajo,
        FechaCreacion = producto.FechaCreacion,
        FechaActualizacion = producto.FechaActualizacion
    };
}
