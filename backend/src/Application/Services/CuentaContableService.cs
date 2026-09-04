using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Interfaces.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public sealed class CuentaContableService : ICuentaContableService
{
    private readonly ICuentaContableRepository _repository;

    public CuentaContableService(ICuentaContableRepository repository)
    {
        _repository = repository;
    }

    public async Task<CuentaContableDto?> GetByIdAsync(int id)
    {
        var cuentas = await _repository.GetAllAsync();
        return BuildTree(cuentas).SelectMany(Flatten).FirstOrDefault(x => x.Id == id);
    }

    public async Task<IReadOnlyList<CuentaContableDto>> GetAllAsync() =>
        BuildTree(await _repository.GetAllAsync());

    public async Task<IReadOnlyList<CuentaContableDto>> GetRaicesAsync() =>
        BuildTree(await _repository.GetAllAsync());

    public async Task<CuentaContableDto> CreateAsync(CreateCuentaContableDto dto)
    {
        var codigo = NormalizeRequired(dto.Codigo, "El código de la cuenta es obligatorio.");
        var nombre = NormalizeRequired(dto.Nombre, "El nombre de la cuenta es obligatorio.");
        ValidateType(dto.Tipo);
        var padre = await ResolveParentAsync(dto.CuentaPadreId, dto.Tipo);

        if (await _repository.GetByCodigoAsync(codigo) is not null)
            throw new ConflictException($"Ya existe una cuenta con el código {codigo}.");

        var cuenta = new CuentaContable
        {
            Codigo = codigo,
            Nombre = nombre,
            Descripcion = NormalizeOptional(dto.Descripcion),
            Tipo = dto.Tipo,
            CuentaPadreId = padre?.Id,
            AceptaMovimientos = dto.AceptaMovimientos,
            Activa = dto.Activa
        };

        await _repository.AddAsync(cuenta);
        await _repository.SaveChangesAsync();
        return MapToDto(cuenta);
    }

    public async Task<CuentaContableDto> UpdateAsync(int id, UpdateCuentaContableDto dto)
    {
        var cuenta = await _repository.GetByIdAsync(id)
            ?? throw new ResourceNotFoundException($"No se encontró la cuenta contable con Id {id}.");
        var codigo = NormalizeRequired(dto.Codigo, "El código de la cuenta es obligatorio.");
        var nombre = NormalizeRequired(dto.Nombre, "El nombre de la cuenta es obligatorio.");
        ValidateType(dto.Tipo);

        var duplicate = await _repository.GetByCodigoAsync(codigo);
        if (duplicate is not null && duplicate.Id != id)
            throw new ConflictException($"Ya existe una cuenta con el código {codigo}.");

        var parent = await ResolveParentAsync(dto.CuentaPadreId, dto.Tipo, id);

        cuenta.Codigo = codigo;
        cuenta.Nombre = nombre;
        cuenta.Descripcion = NormalizeOptional(dto.Descripcion);
        cuenta.Tipo = dto.Tipo;
        cuenta.CuentaPadreId = parent?.Id;
        cuenta.AceptaMovimientos = dto.AceptaMovimientos;
        cuenta.Activa = dto.Activa;
        _repository.Update(cuenta);
        await _repository.SaveChangesAsync();
        return MapToDto(cuenta);
    }

    private async Task<CuentaContable?> ResolveParentAsync(int? parentId, TipoCuentaContable type, int? selfId = null)
    {
        if (!parentId.HasValue)
            return null;
        if (selfId == parentId.Value)
            throw new BusinessRuleException("Una cuenta no puede ser su propia cuenta padre.");

        var parent = await _repository.GetByIdAsync(parentId.Value)
            ?? throw new ResourceNotFoundException("La cuenta padre no existe.");
        if (parent.Tipo != type)
            throw new BusinessRuleException("El tipo de la subcuenta debe coincidir con el tipo de la cuenta padre.");
        return parent;
    }

    private static void ValidateType(TipoCuentaContable type)
    {
        if (!Enum.IsDefined(type))
            throw new BusinessRuleException("El tipo de cuenta no es válido.");
    }

    private static string NormalizeRequired(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BusinessRuleException(message);
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CuentaContableDto MapToDto(CuentaContable entity) => new()
    {
        Id = entity.Id,
        Codigo = entity.Codigo,
        Nombre = entity.Nombre,
        Descripcion = entity.Descripcion,
        Tipo = entity.Tipo,
        CuentaPadreId = entity.CuentaPadreId,
        AceptaMovimientos = entity.AceptaMovimientos,
        Activa = entity.Activa,
        EsRaiz = entity.EsRaiz,
        Subcuentas = entity.Subcuentas.Select(MapToDto).ToList()
    };

    private static List<CuentaContableDto> BuildTree(IEnumerable<CuentaContable> entities)
    {
        var nodes = entities.ToDictionary(entity => entity.Id, MapToDto);
        foreach (var node in nodes.Values)
        {
            if (node.CuentaPadreId is int parentId && nodes.TryGetValue(parentId, out var parent))
                parent.Subcuentas.Add(node);
        }

        return nodes.Values
            .Where(node => node.CuentaPadreId is null || !nodes.ContainsKey(node.CuentaPadreId.Value))
            .OrderBy(node => node.Codigo)
            .ToList();
    }

    private static IEnumerable<CuentaContableDto> Flatten(CuentaContableDto node)
    {
        yield return node;
        foreach (var child in node.Subcuentas.SelectMany(Flatten))
            yield return child;
    }
}
