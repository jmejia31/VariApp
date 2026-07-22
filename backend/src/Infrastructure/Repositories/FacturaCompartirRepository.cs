using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class FacturaCompartirRepository : IFacturaCompartirRepository
{
    private readonly AppDbContext _context;

    public FacturaCompartirRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EnlacePublicoFactura?> GetPorTokenHashAsync(string tokenHash) =>
        await _context.EnlacesPublicosFactura
            .FirstOrDefaultAsync(e => e.Token == tokenHash);

    public async Task<int> ExpirarVigentesAsync(int facturaId, DateTime fechaExpiracion)
    {
        var enlaces = await _context.EnlacesPublicosFactura
            .Where(e => e.FacturaId == facturaId && e.FechaExpiracion > fechaExpiracion)
            .ToListAsync();

        foreach (var enlace in enlaces)
            enlace.FechaExpiracion = fechaExpiracion;

        return enlaces.Count;
    }

    public async Task AddEnlaceAsync(EnlacePublicoFactura enlace) =>
        await _context.EnlacesPublicosFactura.AddAsync(enlace);

    public void UpdateEnlace(EnlacePublicoFactura enlace) =>
        _context.EnlacesPublicosFactura.Update(enlace);

    public async Task AddHistorialAsync(HistorialEnvioFactura historial) =>
        await _context.HistorialEnviosFactura.AddAsync(historial);

    public async Task<List<HistorialEnvioFactura>> GetHistorialAsync(int facturaId) =>
        await _context.HistorialEnviosFactura
            .Where(h => h.FacturaId == facturaId)
            .OrderByDescending(h => h.Fecha)
            .ToListAsync();

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}