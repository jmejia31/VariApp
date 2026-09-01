using InventoryApp.Domain.Entities.Bancos;

namespace InventoryApp.Application.Interfaces;

public interface ICuentaBancariaRepository
{
    Task<CuentaBancaria?> GetByIdAsync(int id);
    Task<List<CuentaBancaria>> GetAllAsync();
    Task<List<CuentaBancaria>> GetActivasAsync();
    Task AddAsync(CuentaBancaria cuenta);
    void Update(CuentaBancaria cuenta);
    Task<int> SaveChangesAsync();
}
