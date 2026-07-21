using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _context;

    public ClienteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cliente?> GetByIdAsync(int id) =>
        await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id && !c.Eliminado);

    public async Task<Cliente?> GetByIdConVentasAsync(int id) =>
        await _context.Clientes.Include(c => c.Ventas).FirstOrDefaultAsync(c => c.Id == id && !c.Eliminado);

    public async Task<List<Cliente>> GetAllAsync() =>
        await _context.Clientes.Where(c => !c.Eliminado).Include(c => c.Ventas).OrderBy(c => c.Nombre).ToListAsync();

    public async Task<List<Cliente>> GetActivosAsync() =>
        await _context.Clientes.Where(c => c.Activo && !c.Eliminado).OrderBy(c => c.Nombre).ToListAsync();

    public async Task<List<Cliente>> BuscarActivosAsync(string termino, int limite = 10)
    {
        var normalizado = termino.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(normalizado)) return new List<Cliente>();

        return await _context.Clientes
            .Where(c => c.Activo && !c.Eliminado && (
                c.Nombre.ToLower().Contains(normalizado) ||
                (c.IdentidadORTN != null && c.IdentidadORTN.ToLower().Contains(normalizado)) ||
                (c.Correo != null && c.Correo.ToLower().Contains(normalizado)) ||
                (c.Telefono != null && c.Telefono.ToLower().Contains(normalizado))))
            .OrderBy(c => c.Nombre)
            .Take(limite)
            .ToListAsync();
    }

    public async Task<Cliente?> BuscarCoincidenciaActivaAsync(string? identidadORTN, string? correo, string? telefono, string? nombre)
    {
        var identidad = NormalizarDocumento(identidadORTN);
        var email = NormalizarTexto(correo);
        var tel = NormalizarDocumento(telefono);
        var nom = NormalizarTexto(nombre);

        return await _context.Clientes
            .Where(c => c.Activo && !c.Eliminado)
            .OrderBy(c => c.Nombre)
            .FirstOrDefaultAsync(c =>
                (!string.IsNullOrEmpty(identidad) && c.IdentidadORTN != null && c.IdentidadORTN.Replace("-", "").Replace(" ", "").ToLower() == identidad) ||
                (!string.IsNullOrEmpty(email) && c.Correo != null && c.Correo.ToLower() == email) ||
                (!string.IsNullOrEmpty(tel) && c.Telefono != null && c.Telefono.Replace("-", "").Replace(" ", "").ToLower() == tel) ||
                (!string.IsNullOrEmpty(nom) && c.Nombre.ToLower() == nom));
    }

    public async Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null) =>
        await _context.Clientes.AnyAsync(c =>
            !c.Eliminado && c.Nombre.ToLower() == nombre.ToLower() && (excluirId == null || c.Id != excluirId));

    public async Task AddAsync(Cliente cliente) =>
        await _context.Clientes.AddAsync(cliente);

    public void Update(Cliente cliente) =>
        _context.Clientes.Update(cliente);

    public void Remove(Cliente cliente) =>
        _context.Clientes.Remove(cliente);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;

    private static string NormalizarTexto(string? value) => value?.Trim().ToLower() ?? string.Empty;

    private static string NormalizarDocumento(string? value) =>
        value?.Replace("-", "").Replace(" ", "").Trim().ToLower() ?? string.Empty;
}
