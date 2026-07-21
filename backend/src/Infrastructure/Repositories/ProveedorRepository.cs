using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class ProveedorRepository : IProveedorRepository
{
    private readonly AppDbContext _context;

    public ProveedorRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Proveedor?> GetByIdAsync(int id) =>
        await _context.Proveedores.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Proveedor?> GetByIdConComprasAsync(int id) =>
        await _context.Proveedores.Include(p => p.Compras).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<Proveedor>> GetAllAsync() =>
        await _context.Proveedores.Include(p => p.Compras).OrderBy(p => p.Nombre).ToListAsync();

    public async Task<List<Proveedor>> GetActivosAsync() =>
        await _context.Proveedores.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();

    public async Task<List<Proveedor>> BuscarActivosAsync(string termino, int limite = 10)
    {
        var normalizado = termino.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(normalizado)) return new List<Proveedor>();

        return await _context.Proveedores
            .Where(p => p.Activo && (
                p.Nombre.ToLower().Contains(normalizado) ||
                (p.Documento != null && p.Documento.ToLower().Contains(normalizado)) ||
                (p.Correo != null && p.Correo.ToLower().Contains(normalizado)) ||
                (p.Telefono != null && p.Telefono.ToLower().Contains(normalizado))))
            .OrderBy(p => p.Nombre)
            .Take(limite)
            .ToListAsync();
    }

    public async Task<Proveedor?> BuscarCoincidenciaActivaAsync(string? documento, string? correo, string? telefono, string? nombre)
    {
        var doc = NormalizarDocumento(documento);
        var email = NormalizarTexto(correo);
        var tel = NormalizarDocumento(telefono);
        var nom = NormalizarTexto(nombre);

        return await _context.Proveedores
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .FirstOrDefaultAsync(p =>
                (!string.IsNullOrEmpty(doc) && p.Documento != null && p.Documento.Replace("-", "").Replace(" ", "").ToLower() == doc) ||
                (!string.IsNullOrEmpty(email) && p.Correo != null && p.Correo.ToLower() == email) ||
                (!string.IsNullOrEmpty(tel) && p.Telefono != null && p.Telefono.Replace("-", "").Replace(" ", "").ToLower() == tel) ||
                (!string.IsNullOrEmpty(nom) && p.Nombre.ToLower() == nom));
    }

    public async Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null) =>
        await _context.Proveedores.AnyAsync(p =>
            p.Nombre.ToLower() == nombre.ToLower() && (excluirId == null || p.Id != excluirId));

    public async Task AddAsync(Proveedor proveedor) =>
        await _context.Proveedores.AddAsync(proveedor);

    public void Update(Proveedor proveedor) =>
        _context.Proveedores.Update(proveedor);

    public void Remove(Proveedor proveedor) =>
        _context.Proveedores.Remove(proveedor);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;

    private static string NormalizarTexto(string? value) => value?.Trim().ToLower() ?? string.Empty;

    private static string NormalizarDocumento(string? value) =>
        value?.Replace("-", "").Replace(" ", "").Trim().ToLower() ?? string.Empty;
}
