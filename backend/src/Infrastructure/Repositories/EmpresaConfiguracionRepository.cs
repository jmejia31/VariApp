using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class EmpresaConfiguracionRepository : IEmpresaConfiguracionRepository
{
    private readonly AppDbContext _context;

    public EmpresaConfiguracionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EmpresaConfiguracion?> GetActivaAsync() =>
        await _context.EmpresaConfiguraciones.FirstOrDefaultAsync(e => e.Activa);

    public async Task AddAsync(EmpresaConfiguracion config) =>
        await _context.EmpresaConfiguraciones.AddAsync(config);

    public void Update(EmpresaConfiguracion config) =>
        _context.EmpresaConfiguraciones.Update(config);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
