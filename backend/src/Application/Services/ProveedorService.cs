using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class ProveedorService : IProveedorService
{
    private readonly IProveedorRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public ProveedorService(IProveedorRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<List<ProveedorDto>> GetAllAsync()
    {
        var proveedores = await _repository.GetAllAsync();
        return proveedores.Select(p => ToDto(p)).ToList();
    }

    public async Task<List<ProveedorDto>> GetActivosAsync()
    {
        var proveedores = await _repository.GetActivosAsync();
        return proveedores.Select(p => ToDto(p, incluirCompras: false)).ToList();
    }

    public async Task<ProveedorDto?> GetByIdAsync(int id)
    {
        var proveedor = await _repository.GetByIdConComprasAsync(id);
        return proveedor is null ? null : ToDto(proveedor);
    }

    public async Task<ProveedorDto> CreateAsync(CreateProveedorDto dto)
    {
        var nombre = dto.Nombre.Trim();
        if (await _repository.ExisteNombreAsync(nombre))
            throw new BusinessRuleException($"Ya existe un proveedor con el nombre '{nombre}'.");

        var proveedor = new Proveedor
        {
            Nombre = nombre,
            Telefono = dto.Telefono,
            Documento = dto.Documento,
            Correo = dto.Correo,
            Direccion = dto.Direccion,
            Activo = true,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await _repository.AddAsync(proveedor);
        await _repository.SaveChangesAsync();

        return ToDto(proveedor);
    }

    public async Task<ProveedorDto?> UpdateAsync(int id, UpdateProveedorDto dto)
    {
        var proveedor = await _repository.GetByIdAsync(id);
        if (proveedor is null) return null;

        var nombre = dto.Nombre.Trim();
        if (await _repository.ExisteNombreAsync(nombre, id))
            throw new BusinessRuleException($"Ya existe un proveedor con el nombre '{nombre}'.");

        proveedor.Nombre = nombre;
        proveedor.Telefono = dto.Telefono;
        proveedor.Documento = dto.Documento;
        proveedor.Correo = dto.Correo;
        proveedor.Direccion = dto.Direccion;
        proveedor.Activo = dto.Activo;
        proveedor.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        proveedor.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        proveedor.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(proveedor);
        await _repository.SaveChangesAsync();

        return ToDto(proveedor);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var proveedor = await _repository.GetByIdConComprasAsync(id);
        if (proveedor is null) return false;

        if (proveedor.Compras.Any())
        {
            // Mismo patrón que Categorías: si tiene historial, se desactiva en vez de borrar.
            proveedor.Activo = false;
            proveedor.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
            proveedor.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
            proveedor.FechaActualizacion = DateTime.UtcNow;
            _repository.Update(proveedor);
            await _repository.SaveChangesAsync();

            throw new BusinessRuleException(
                $"El proveedor tiene {proveedor.Compras.Count} compra(s) registrada(s); no se puede eliminar. Se desactivó en su lugar.");
        }

        _repository.Remove(proveedor);
        return await _repository.SaveChangesAsync();
    }

    private static ProveedorDto ToDto(Proveedor p, bool incluirCompras = true) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Telefono = p.Telefono,
        Documento = p.Documento,
        Correo = p.Correo,
        Direccion = p.Direccion,
        Activo = p.Activo,
        TotalCompras = incluirCompras ? p.Compras?.Count(c => c.Estado != EstadoDocumento.Anulada) ?? 0 : 0,
        TotalComprado = incluirCompras ? p.Compras?.Where(c => c.Estado == EstadoDocumento.Confirmada).Sum(c => c.Total) ?? 0 : 0,
        CreadoPorNombreUsuario = p.CreadoPorNombreUsuario,
        FechaCreacion = p.FechaCreacion
    };
}
