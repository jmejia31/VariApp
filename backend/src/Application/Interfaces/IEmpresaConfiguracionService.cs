using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace InventoryApp.Application.Interfaces;

public interface IEmpresaConfiguracionService
{
    Task<EmpresaConfiguracionDto> GetActivaAsync();
    Task<EmpresaConfiguracion> GetActivaEntidadAsync();
    Task<EmpresaConfiguracionDto> UpdateAsync(UpdateEmpresaConfiguracionDto dto);
    Task<EmpresaConfiguracionDto> UpdateLogoAsync(IFormFile logo);
    Task<EmpresaConfiguracionDto> RestaurarLogoAsync();
}
