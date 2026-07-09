using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

public class CategoriaService : ICategoriaService
{
    private readonly ICategoriaRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public CategoriaService(ICategoriaRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<List<CategoriaDto>> GetAllAsync()
    {
        var categorias = await _repository.GetAllAsync();
        return categorias.Select(c => ToDto(c)).ToList();
    }

    public async Task<List<CategoriaDto>> GetActivasAsync()
    {
        var categorias = await _repository.GetActivasAsync();
        return categorias.Select(c => ToDto(c, incluirConteo: false)).ToList();
    }

    public async Task<CategoriaDto?> GetByIdAsync(int id)
    {
        var categoria = await _repository.GetByIdConProductosAsync(id);
        return categoria is null ? null : ToDto(categoria);
    }

    public async Task<CategoriaDto> CreateAsync(CreateCategoriaDto dto)
    {
        if (await _repository.ExisteNombreAsync(dto.Nombre))
            throw new BusinessRuleException($"Ya existe una categoría con el nombre '{dto.Nombre}'.");

        var categoria = new Categoria
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion,
            Activa = true,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await _repository.AddAsync(categoria);
        await _repository.SaveChangesAsync();

        return ToDto(categoria);
    }

    public async Task<CategoriaDto?> UpdateAsync(int id, UpdateCategoriaDto dto)
    {
        var categoria = await _repository.GetByIdAsync(id);
        if (categoria is null) return null;

        if (await _repository.ExisteNombreAsync(dto.Nombre, id))
            throw new BusinessRuleException($"Ya existe una categoría con el nombre '{dto.Nombre}'.");

        categoria.Nombre = dto.Nombre.Trim();
        categoria.Descripcion = dto.Descripcion;
        categoria.Activa = dto.Activa;
        categoria.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        categoria.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        categoria.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(categoria);
        await _repository.SaveChangesAsync();

        return ToDto(categoria);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var categoria = await _repository.GetByIdConProductosAsync(id);
        if (categoria is null) return false;

        if (categoria.Productos.Any())
        {
            // Regla: si tiene productos asociados, no se elimina físicamente; se desactiva.
            categoria.Activa = false;
            categoria.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
            categoria.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
            categoria.FechaActualizacion = DateTime.UtcNow;
            _repository.Update(categoria);
            await _repository.SaveChangesAsync();

            throw new BusinessRuleException(
                $"La categoría tiene {categoria.Productos.Count} producto(s) asociado(s); no se puede eliminar. Se desactivó en su lugar.");
        }

        _repository.Remove(categoria);
        return await _repository.SaveChangesAsync();
    }

    private static CategoriaDto ToDto(Categoria c, bool incluirConteo = true) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Descripcion = c.Descripcion,
        Activa = c.Activa,
        TotalProductos = incluirConteo ? c.Productos?.Count ?? 0 : 0,
        CreadoPorNombreUsuario = c.CreadoPorNombreUsuario,
        ActualizadoPorNombreUsuario = c.ActualizadoPorNombreUsuario,
        FechaCreacion = c.FechaCreacion,
        FechaActualizacion = c.FechaActualizacion
    };
}
