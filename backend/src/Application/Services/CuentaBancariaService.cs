using InventoryApp.Application.DTOs.Bancos;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities.Bancos;
using InventoryApp.Application.Bancos;

namespace InventoryApp.Application.Services;

public sealed class CuentaBancariaService : ICuentaBancariaService
{
    private readonly ICuentaBancariaRepository _repository;

    public CuentaBancariaService(ICuentaBancariaRepository repository)
    {
        _repository = repository;
    }

    public async Task<CuentaBancariaDto?> GetByIdAsync(int id)
    {
        var cuenta = await _repository.GetByIdAsync(id);
        if (cuenta == null) return null;

        return MapToDto(cuenta);
    }

    public async Task<CuentaBancariaPage<CuentaBancariaDto>> GetAllAsync(CuentaBancariaQueryFilter filter)
    {
        var page = await _repository.GetAllAsync(filter);
        var dtoItems = page.Items.Select(MapToDto).ToList();
        return new CuentaBancariaPage<CuentaBancariaDto>(dtoItems, page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<List<CuentaBancariaDto>> GetActivasAsync()
    {
        var cuentas = await _repository.GetActivasAsync();
        return cuentas.Select(MapToDto).ToList();
    }

    public async Task<CuentaBancariaDto> AddAsync(CreateCuentaBancariaDto dto)
    {
        var cuenta = new CuentaBancaria(
            dto.BancoId,
            dto.Nombre,
            dto.NumeroCuenta,
            dto.Moneda,
            dto.SaldoInicial);

        await _repository.AddAsync(cuenta);
        await _repository.SaveChangesAsync();

        return MapToDto(cuenta);
    }

    public async Task ActivarAsync(int id)
    {
        var cuenta = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"No se encontró la cuenta con Id {id}.");

        cuenta.Activar();
        _repository.Update(cuenta);
        await _repository.SaveChangesAsync();
    }

    public async Task DesactivarAsync(int id)
    {
        var cuenta = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"No se encontró la cuenta con Id {id}.");

        cuenta.Desactivar();
        _repository.Update(cuenta);
        await _repository.SaveChangesAsync();
    }

    private static CuentaBancariaDto MapToDto(CuentaBancaria cuenta) => new()
    {
        Id = cuenta.Id,
        BancoId = cuenta.BancoId,
        Nombre = cuenta.Nombre,
        NumeroCuenta = cuenta.NumeroCuenta,
        Moneda = cuenta.Moneda,
        SaldoInicial = cuenta.SaldoInicial,
        Estado = cuenta.Estado
    };
}
