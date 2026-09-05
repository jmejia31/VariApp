using InventoryApp.Domain.Entities.Contabilidad;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public sealed class PeriodoContableRepository
{
    private readonly AppDbContext _context;

    public PeriodoContableRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PeriodoContable?> GetByIdAsync(int id, bool tracking = false, CancellationToken cancellationToken = default)
    {
        var query = _context.PeriodosContables.AsQueryable();
        if (!tracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<PeriodoContable?> GetByDateAsync(DateTime date, bool tracking = false, CancellationToken cancellationToken = default)
    {
        var query = _context.PeriodosContables.AsQueryable();
        if (!tracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(p => p.FechaInicio <= date && p.FechaFin >= date, cancellationToken);
    }

    public async Task<bool> IsValidDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        return await _context.PeriodosContables
            .AnyAsync(p => p.Estado == EstadoPeriodoContable.Abierto &&
                           p.FechaInicio <= date && p.FechaFin >= date, cancellationToken);
    }

    public async Task<bool> HasOverlappingPeriodAsync(DateTime fechaInicio, DateTime fechaFin, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _context.PeriodosContables
            .AnyAsync(p => (excludeId == null || p.Id != excludeId) &&
                           p.FechaInicio <= fechaFin && p.FechaFin >= fechaInicio,
                      cancellationToken);
    }

    public async Task<List<PeriodoContable>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PeriodosContables
            .OrderByDescending(p => p.FechaInicio)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(PeriodoContable periodo, CancellationToken cancellationToken = default)
    {
        return _context.PeriodosContables.AddAsync(periodo, cancellationToken).AsTask();
    }

    public void Update(PeriodoContable periodo)
    {
        _context.PeriodosContables.Update(periodo);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
