using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IEmpresaConfiguracionService
{
    Task<EmpresaConfiguracionDto> GetActivaAsync();
    Task<EmpresaConfiguracion> GetActivaEntidadAsync();
    Task<EmpresaConfiguracionDto> UpdateAsync(UpdateEmpresaConfiguracionDto dto);
}
