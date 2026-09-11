using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IEmpresaConfiguracionRepository
{
    Task<EmpresaConfiguracion?> GetActivaAsync();
    Task AddAsync(EmpresaConfiguracion config);
    void Update(EmpresaConfiguracion config);
    Task<bool> SaveChangesAsync();
}
