using InventoryApp.Domain.Entities.Bancos;
using InventoryApp.Application.Bancos;

namespace InventoryApp.Application.Interfaces;

public interface ICuentaBancariaRepository
{
    Task<CuentaBancaria?> GetByIdAsync(int id);
    Task<CuentaBancariaPage<CuentaBancaria>> GetAllAsync(CuentaBancariaQueryFilter filter);
    Task<List<CuentaBancaria>> GetActivasAsync();
    Task AddAsync(CuentaBancaria cuenta);
    void Update(CuentaBancaria cuenta);
    Task<int> SaveChangesAsync();
}
