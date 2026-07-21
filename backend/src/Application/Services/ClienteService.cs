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
    private readonly IAuditoriaService _auditoria;

    public ClienteService(IClienteRepository repository, ICurrentUserService currentUser, IAuditoriaService auditoria)
    {
        _repository = repository;
        _currentUser = currentUser;
        _auditoria = auditoria;
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

    public async Task<List<ClienteDto>> BuscarActivosAsync(string termino)
    {
        var clientes = await _repository.BuscarActivosAsync(termino);
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
        await _auditoria.RegistrarAsync(ModuloSistema.Clientes, AccionPermiso.Crear, $"Cliente creado: {cliente.Nombre}", cliente.Id);

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
        await _auditoria.RegistrarAsync(ModuloSistema.Clientes, AccionPermiso.Editar, $"Cliente actualizado: {cliente.Nombre}", cliente.Id);

        return ToDto(cliente);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var cliente = await _repository.GetByIdConVentasAsync(id);
        if (cliente is null) return false;

        cliente.Activo = false;
        cliente.Eliminado = true;
        cliente.FechaEliminacion = DateTime.UtcNow;
        cliente.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        cliente.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        cliente.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        cliente.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(cliente);
        var eliminado = await _repository.SaveChangesAsync();
        if (eliminado)
            await _auditoria.RegistrarAsync(ModuloSistema.Clientes, AccionPermiso.EliminarLogico, $"Cliente eliminado logicamente: {cliente.Nombre}", id);
        return eliminado;
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
