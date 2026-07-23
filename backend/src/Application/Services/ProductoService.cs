using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Mappings;
using InventoryApp.Application.Validators;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class ProductoService : IProductoService
{
    private const int MaxImagenes = 5;

    private readonly IProductoRepository _repository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IImageStorageService _imageStorage;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;

    public ProductoService(
        IProductoRepository repository,
        ICategoriaRepository categoriaRepository,
        IImageStorageService imageStorage,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria)
    {
        _repository = repository;
        _categoriaRepository = categoriaRepository;
        _imageStorage = imageStorage;
        _currentUser = currentUser;
        _auditoria = auditoria;
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
            Activo = true,
            Eliminado = false,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        for (var i = 0; i < imagenes.Count; i++)
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

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Crear,
            $"Producto creado: {producto.Nombre}.",
            producto.Id,
            entidad: "Producto",
            valoresNuevos: new
            {
                producto.Nombre,
                producto.Marca,
                producto.Modelo,
                producto.Cantidad,
                producto.Costo,
                producto.Precio,
                Imagenes = producto.Imagenes.Count
            });

        return ProductoMapper.ToDto(producto);
    }

    public async Task<ProductoDto?> UpdateAsync(int id, UpdateProductoDto dto)
    {
        var producto = await _repository.GetByIdAsync(id);
        if (producto is null) return null;

        var valoresAnteriores = new
        {
            producto.Nombre,
            producto.Marca,
            producto.Modelo,
            producto.Descripcion,
            producto.Cantidad,
            producto.Costo,
            producto.Precio,
            producto.UmbralStockBajo,
            producto.CategoriaId,
            Imagenes = producto.Imagenes.Count,
            ImagenPrincipalId = producto.ImagenPrincipal?.Id
        };

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

        if (dto.ImagenesAEliminarIds is { Count: > 0 })
        {
            var aEliminar = producto.Imagenes
                .Where(i => dto.ImagenesAEliminarIds.Contains(i.Id))
                .ToList();
            foreach (var imagen in aEliminar)
            {
                await _imageStorage.DeleteAsync(imagen.PublicId);
                producto.Imagenes.Remove(imagen);
            }
        }

        var nuevas = dto.ImagenesNuevas ?? new List<Microsoft.AspNetCore.Http.IFormFile>();
        if (producto.Imagenes.Count + nuevas.Count > MaxImagenes)
            throw new BusinessRuleException(
                $"Un producto puede tener máximo {MaxImagenes} fotos ({producto.Imagenes.Count} existentes + {nuevas.Count} nuevas excede el límite).");
        ValidarImagenes(nuevas);

        var siguienteOrden = producto.Imagenes.Count == 0
            ? 0
            : producto.Imagenes.Max(i => i.Orden) + 1;
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

        if (dto.ImagenPrincipalId.HasValue)
        {
            var nuevaPrincipal = producto.Imagenes
                .FirstOrDefault(i => i.Id == dto.ImagenPrincipalId.Value);
            if (nuevaPrincipal is null)
                throw new BusinessRuleException("La imagen indicada como principal no pertenece a este producto.");

            foreach (var imagen in producto.Imagenes)
                imagen.EsPrincipal = false;
            nuevaPrincipal.EsPrincipal = true;
        }
        else if (producto.Imagenes.Count > 0 && !producto.Imagenes.Any(i => i.EsPrincipal))
        {
            producto.Imagenes.OrderBy(i => i.Orden).First().EsPrincipal = true;
        }

        _repository.Update(producto);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Editar,
            $"Producto actualizado: {producto.Nombre}.",
            producto.Id,
            entidad: "Producto",
            valoresAnteriores: valoresAnteriores,
            valoresNuevos: new
            {
                producto.Nombre,
                producto.Marca,
                producto.Modelo,
                producto.Descripcion,
                producto.Cantidad,
                producto.Costo,
                producto.Precio,
                producto.UmbralStockBajo,
                producto.CategoriaId,
                Imagenes = producto.Imagenes.Count,
                ImagenPrincipalId = producto.ImagenPrincipal?.Id
            });

        return ProductoMapper.ToDto(producto);
    }

    public async Task<ProductoDto?> CambiarEstadoAsync(int id, bool activo)
    {
        var producto = await _repository.GetByIdAsync(id);
        if (producto is null) return null;
        if (producto.Activo == activo) return ProductoMapper.ToDto(producto);

        var estadoAnterior = producto.Activo;
        producto.Activo = activo;
        producto.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        producto.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        producto.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(producto);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            activo ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"Producto {(activo ? "activado" : "desactivado")}: {producto.Nombre}.",
            producto.Id,
            entidad: "Producto",
            valoresAnteriores: new { Activo = estadoAnterior },
            valoresNuevos: new { producto.Activo });

        return ProductoMapper.ToDto(producto);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var producto = await _repository.GetByIdAsync(id);
        if (producto is null) return false;

        var valoresAnteriores = new
        {
            producto.Nombre,
            producto.Marca,
            producto.Modelo,
            producto.Activo,
            producto.Eliminado,
            Imagenes = producto.Imagenes.Count
        };

        producto.Activo = false;
        producto.Eliminado = true;
        producto.FechaEliminacion = DateTime.UtcNow;
        producto.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        producto.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        producto.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        producto.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(producto);
        var guardado = await _repository.SaveChangesAsync();
        if (guardado)
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.Productos,
                AccionPermiso.EliminarLogico,
                $"Producto eliminado lógicamente: {producto.Nombre}.",
                id,
                entidad: "Producto",
                valoresAnteriores: valoresAnteriores,
                valoresNuevos: new
                {
                    producto.Activo,
                    producto.Eliminado,
                    producto.FechaEliminacion
                });
        }

        return guardado;
    }

    private static void ValidarImagenes(IEnumerable<Microsoft.AspNetCore.Http.IFormFile> imagenes)
    {
        if (imagenes.Any(imagen => !ImagenValidationHelper.EsImagenValida(imagen)))
            throw new BusinessRuleException("Solo se permiten imágenes JPG, PNG o WebP de hasta 5 MB.");
    }
}
