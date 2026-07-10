using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

public class EmpresaConfiguracionService : IEmpresaConfiguracionService
{
    private readonly IEmpresaConfiguracionRepository _repository;

    public EmpresaConfiguracionService(IEmpresaConfiguracionRepository repository)
    {
        _repository = repository;
    }

    public async Task<EmpresaConfiguracionDto> GetActivaAsync()
    {
        var config = await GetActivaEntidadAsync();
        return ToDto(config);
    }

    /// Si no existe configuración guardada, retorna los valores por defecto de VariStorehn
    /// (sin persistirlos hasta que el administrador los edite explícitamente).
    public async Task<EmpresaConfiguracion> GetActivaEntidadAsync()
    {
        var config = await _repository.GetActivaAsync();
        return config ?? new EmpresaConfiguracion();
    }

    public async Task<EmpresaConfiguracionDto> UpdateAsync(UpdateEmpresaConfiguracionDto dto)
    {
        var config = await _repository.GetActivaAsync();

        if (config is null)
        {
            config = new EmpresaConfiguracion { Activa = true };
            await _repository.AddAsync(config);
        }

        config.NombreComercial = dto.NombreComercial;
        config.Eslogan = dto.Eslogan;
        config.RTN = dto.RTN;
        config.Telefono = dto.Telefono;
        config.Correo = dto.Correo;
        config.Direccion = dto.Direccion;

        _repository.Update(config);
        await _repository.SaveChangesAsync();

        return ToDto(config);
    }

    private static EmpresaConfiguracionDto ToDto(EmpresaConfiguracion c) => new()
    {
        NombreComercial = c.NombreComercial,
        Eslogan = c.Eslogan,
        RTN = c.RTN,
        Telefono = c.Telefono,
        Correo = c.Correo,
        Direccion = c.Direccion,
        LogoUrl = c.LogoUrl
    };
}
