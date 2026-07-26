using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Repositories;

public class CatalogoProductoRepository : ICatalogoProductoRepository
{
    private readonly AppDbContext _context;

    public CatalogoProductoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CatalogoProducto>> GetAllAsync(
        TipoCatalogoProducto tipo,
        string? buscar = null,
        int? catalogoPadreId = null)
    {
        var query = _context.CatalogosProducto
            .Include(c => c.CatalogoPadre)
            .Include(c => c.ElementosHijos)
            .AsQueryable()
            .Where(c => c.Tipo == tipo);

        if (catalogoPadreId.HasValue)
            query = query.Where(c => c.CatalogoPadreId == catalogoPadreId);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var termino = buscar.Trim();
            query = query.Where(c => c.Nombre.Contains(termino) ||
                                     (c.Descripcion != null && c.Descripcion.Contains(termino)));
        }

        return await query
            .OrderBy(c => c.Orden)
            .ThenBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<List<CatalogoProducto>> GetActivosAsync(
        TipoCatalogoProducto tipo,
        int? catalogoPadreId = null)
    {
        var query = _context.CatalogosProducto
            .Include(c => c.CatalogoPadre)
            .Where(c => c.Tipo == tipo && c.Activo);

        if (catalogoPadreId.HasValue)
            query = query.Where(c => c.CatalogoPadreId == catalogoPadreId);

        return await query
            .OrderBy(c => c.Orden)
            .ThenBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<CatalogoProducto?> GetByIdAsync(int id) =>
        await _context.CatalogosProducto.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<CatalogoProducto?> GetByIdConRelacionesAsync(int id) =>
        await _context.CatalogosProducto
            .Include(c => c.CatalogoPadre)
            .Include(c => c.ElementosHijos)
            .Include(c => c.ProductosComoColor)
            .Include(c => c.ProductosComoTalla)
            .Include(c => c.ProductosComoMarca)
            .Include(c => c.ProductosComoModelo)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<bool> ExisteNombreAsync(
        TipoCatalogoProducto tipo,
        string nombre,
        int? catalogoPadreId,
        int? excluirId = null)
    {
        var normalizado = nombre.Trim().ToLower();
        return await _context.CatalogosProducto.AnyAsync(c =>
            c.Tipo == tipo &&
            c.Nombre.ToLower() == normalizado &&
            c.CatalogoPadreId == catalogoPadreId &&
            (!excluirId.HasValue || c.Id != excluirId.Value));
    }

    public async Task AddAsync(CatalogoProducto catalogo) =>
        await _context.CatalogosProducto.AddAsync(catalogo);

    public void Update(CatalogoProducto catalogo) =>
        _context.CatalogosProducto.Update(catalogo);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
