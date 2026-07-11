using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public ClienteService(IClienteRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<List<ClienteDto>> GetAllAsync()
    {
        var clientes = await _repository.GetAllAsync();
        return clientes.Select(c => ToDto(c)).ToList();
    }

    public async Task<List<ClienteDto>> GetActivosAsync()
    {
        var clientes = await _repository.GetActivosAsync();
        return clientes.Select(c => ToDto(c, incluirVentas: false)).ToList();
    }

    public async Task<ClienteDto?> GetByIdAsync(int id)
    {
        var cliente = await _repository.GetByIdConVentasAsync(id);
        return cliente is null ? null : ToDto(cliente);
    }

    public async Task<ClienteDto> CreateAsync(CreateClienteDto dto)
    {
        var nombre = dto.Nombre.Trim();
        if (await _repository.ExisteNombreAsync(nombre))
            throw new BusinessRuleException($"Ya existe un cliente con el nombre '{nombre}'.");

        var cliente = new Cliente
        {
            Nombre = nombre,
            Telefono = dto.Telefono,
            IdentidadORTN = dto.IdentidadORTN,
            Correo = dto.Correo,
            Direccion = dto.Direccion,
            Activo = true,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await _repository.AddAsync(cliente);
        await _repository.SaveChangesAsync();

        return ToDto(cliente);
    }

    public async Task<ClienteDto?> UpdateAsync(int id, UpdateClienteDto dto)
    {
        var cliente = await _repository.GetByIdAsync(id);
        if (cliente is null) return null;

        var nombre = dto.Nombre.Trim();
        if (await _repository.ExisteNombreAsync(nombre, id))
            throw new BusinessRuleException($"Ya existe un cliente con el nombre '{nombre}'.");

        cliente.Nombre = nombre;
        cliente.Telefono = dto.Telefono;
        cliente.IdentidadORTN = dto.IdentidadORTN;
        cliente.Correo = dto.Correo;
        cliente.Direccion = dto.Direccion;
        cliente.Activo = dto.Activo;
        cliente.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        cliente.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        cliente.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(cliente);
        await _repository.SaveChangesAsync();

        return ToDto(cliente);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var cliente = await _repository.GetByIdConVentasAsync(id);
        if (cliente is null) return false;

        if (cliente.Ventas.Any())
        {
            cliente.Activo = false;
            cliente.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
            cliente.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
            cliente.FechaActualizacion = DateTime.UtcNow;
            _repository.Update(cliente);
            await _repository.SaveChangesAsync();

            throw new BusinessRuleException(
                $"El cliente tiene {cliente.Ventas.Count} venta(s) registrada(s); no se puede eliminar. Se desactivó en su lugar.");
        }

        _repository.Remove(cliente);
        return await _repository.SaveChangesAsync();
    }

    private static ClienteDto ToDto(Cliente c, bool incluirVentas = true) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Telefono = c.Telefono,
        IdentidadORTN = c.IdentidadORTN,
        Correo = c.Correo,
        Direccion = c.Direccion,
        Activo = c.Activo,
        TotalVentas = incluirVentas ? c.Ventas?.Count(v => v.Estado != EstadoDocumento.Anulada) ?? 0 : 0,
        TotalVendido = incluirVentas ? c.Ventas?.Where(v => v.Estado == EstadoDocumento.Confirmada).Sum(v => v.Total) ?? 0 : 0,
        CreadoPorNombreUsuario = c.CreadoPorNombreUsuario,
        FechaCreacion = c.FechaCreacion
    };
}
