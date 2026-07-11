using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _repository;

    public UsuarioService(IUsuarioRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<UsuarioDto>> GetAllAsync()
    {
        var usuarios = await _repository.GetAllAsync();
        return usuarios.Select(ToDto).ToList();
    }

    public async Task<UsuarioDto> CreateAsync(CreateUsuarioDto dto)
    {
        var existente = await _repository.GetByNombreUsuarioAsync(dto.NombreUsuario);
        if (existente is not null)
            throw new InvalidOperationException("Ya existe un usuario con ese nombre de usuario.");

        if (!Enum.TryParse<RolUsuario>(dto.Rol, out var rol))
            rol = RolUsuario.Vendedor;

        var usuario = new Usuario
        {
            NombreUsuario = dto.NombreUsuario.Trim(),
            NombreCompleto = dto.NombreCompleto.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Rol = rol,
            Activo = true
        };

        await _repository.AddAsync(usuario);
        await _repository.SaveChangesAsync();

        return ToDto(usuario);
    }

    public async Task<UsuarioDto?> UpdateAsync(int id, UpdateUsuarioDto dto)
    {
        var usuario = await _repository.GetByIdAsync(id);
        if (usuario is null) return null;

        if (!Enum.TryParse<RolUsuario>(dto.Rol, out var rol))
            rol = usuario.Rol;

        usuario.NombreCompleto = dto.NombreCompleto.Trim();
        usuario.Rol = rol;

        if (!string.IsNullOrWhiteSpace(dto.NuevaPassword))
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);

        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        return ToDto(usuario);
    }

    public async Task<UsuarioDto?> UpdateEstadoAsync(int id, bool activo)
    {
        var usuario = await _repository.GetByIdAsync(id);
        if (usuario is null) return null;

        usuario.Activo = activo;
        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        return ToDto(usuario);
    }

    private static UsuarioDto ToDto(Usuario u) => new()
    {
        Id = u.Id,
        NombreUsuario = u.NombreUsuario,
        NombreCompleto = u.NombreCompleto,
        Rol = u.Rol.ToString(),
        Activo = u.Activo,
        FechaCreacion = u.FechaCreacion
    };
}
