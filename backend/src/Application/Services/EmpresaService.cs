using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

public sealed class EmpresaService : IEmpresaService
{
    private const int NombreMaxLength = 200;
    private readonly IEmpresaRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public EmpresaService(IEmpresaRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<List<EmpresaDto>> ListAsync(bool? activa = null, CancellationToken cancellationToken = default)
    {
        var empresas = await _repository.ListAsync(activa, cancellationToken);
        return empresas.Select(ToDto).ToList();
    }

    public async Task<EmpresaDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidarId(id);
        var empresa = await _repository.GetByIdAsync(id, cancellationToken);
        return empresa is null ? null : ToDto(empresa);
    }

    public async Task<EmpresaDto> CreateAsync(CreateEmpresaDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var empresa = new Empresa(NormalizarNombre(dto.Nombre))
        {
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await _repository.AddAsync(empresa, cancellationToken);
        if (!await _repository.SaveChangesAsync(cancellationToken))
            throw new BusinessRuleException("No se pudo crear la empresa.");

        return ToDto(empresa);
    }

    public async Task<EmpresaDto?> UpdateAsync(int id, UpdateEmpresaDto dto, CancellationToken cancellationToken = default)
    {
        ValidarId(id);
        ArgumentNullException.ThrowIfNull(dto);
        var empresa = await _repository.GetByIdAsync(id, cancellationToken);
        if (empresa is null) return null;

        empresa.CambiarNombre(NormalizarNombre(dto.Nombre));
        MarcarActualizacion(empresa);
        _repository.Update(empresa);
        if (!await _repository.SaveChangesAsync(cancellationToken))
            throw new BusinessRuleException("No se pudo actualizar la empresa.");

        return ToDto(empresa);
    }

    public async Task<EmpresaDto?> CambiarEstadoAsync(int id, bool activa, CancellationToken cancellationToken = default)
    {
        ValidarId(id);
        var empresa = await _repository.GetByIdAsync(id, cancellationToken);
        if (empresa is null) return null;
        if (empresa.Activa == activa) return ToDto(empresa);

        if (activa) empresa.Activar(); else empresa.Desactivar();
        MarcarActualizacion(empresa);
        _repository.Update(empresa);
        if (!await _repository.SaveChangesAsync(cancellationToken))
            throw new BusinessRuleException("No se pudo cambiar el estado de la empresa.");

        return ToDto(empresa);
    }

    private void MarcarActualizacion(Empresa empresa)
    {
        empresa.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        empresa.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        empresa.FechaActualizacion = DateTime.UtcNow;
    }

    private static string NormalizarNombre(string? nombre)
    {
        var valor = nombre?.Trim() ?? string.Empty;
        if (valor.Length == 0)
            throw new BusinessRuleException("El nombre de la empresa es obligatorio.");
        if (valor.Length > NombreMaxLength)
            throw new BusinessRuleException($"El nombre de la empresa no puede exceder {NombreMaxLength} caracteres.");
        return valor;
    }

    private static void ValidarId(int id)
    {
        if (id <= 0) throw new BusinessRuleException("El identificador de empresa debe ser mayor que cero.");
    }

    private static EmpresaDto ToDto(Empresa empresa) => new()
    {
        Id = empresa.Id,
        Nombre = empresa.Nombre,
        Activa = empresa.Activa,
        FechaCreacion = empresa.FechaCreacion,
        FechaActualizacion = empresa.FechaActualizacion
    };
}
