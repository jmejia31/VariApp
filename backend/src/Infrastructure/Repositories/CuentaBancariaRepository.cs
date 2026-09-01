using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities.Bancos;
using InventoryApp.Domain.Enums.Bancos;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class CuentaBancariaRepository : ICuentaBancariaRepository
{
    private readonly AppDbContext _context;

    public CuentaBancariaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CuentaBancaria?> GetByIdAsync(int id)
    {
        return await _context.CuentasBancarias.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<CuentaBancaria>> GetAllAsync()
    {
        return await _context.CuentasBancarias.ToListAsync();
    }

    public async Task<List<CuentaBancaria>> GetActivasAsync()
    {
        return await _context.CuentasBancarias
            .Where(c => c.Estado == EstadoCuentaBancaria.Activa)
            .ToListAsync();
    }

    public async Task AddAsync(CuentaBancaria cuenta)
    {
        await _context.CuentasBancarias.AddAsync(cuenta);
    }

    public void Update(CuentaBancaria cuenta)
    {
        _context.CuentasBancarias.Update(cuenta);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
