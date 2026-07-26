using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Interfaces;

public interface ICatalogoProductoRepository
{
    Task<List<CatalogoProducto>> GetAllAsync(TipoCatalogoProducto tipo, string? buscar = null, int? catalogoPadreId = null);
    Task<List<CatalogoProducto>> GetActivosAsync(TipoCatalogoProducto tipo, int? catalogoPadreId = null);
    Task<CatalogoProducto?> GetByIdAsync(int id);
    Task<CatalogoProducto?> GetByIdConRelacionesAsync(int id);
    Task<bool> ExisteNombreAsync(TipoCatalogoProducto tipo, string nombre, int? catalogoPadreId, int? excluirId = null);
    Task AddAsync(CatalogoProducto catalogo);
    void Update(CatalogoProducto catalogo);
    Task<bool> SaveChangesAsync();
}
