using InventoryApp.Domain.Common;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IMovimientoInventarioRepository
{
    Task AddAsync(MovimientoInventario movimiento);
    Task AddConOrigenTipadoAsync(MovimientoInventario movimiento, OrigenMovimientoInventario origen);
    Task<List<MovimientoInventario>> GetByProductoAsync(int productoId);
    Task<List<MovimientoInventario>> GetFilteredAsync(int? productoId, string? tipo, DateTime? desde, DateTime? hasta);
    Task<int?> GetUltimoMovimientoOriginalCompraIdAsync(int compraId);
    Task<bool> ExisteMovimientoPosteriorAsync(
        int ultimoMovimientoOriginalId,
        IReadOnlyCollection<int> productoIds);
}
