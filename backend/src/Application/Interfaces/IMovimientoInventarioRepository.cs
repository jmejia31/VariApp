using InventoryApp.Domain.Common;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public sealed record MovimientoInventarioOrigenPersistido(
    int MovimientoId,
    int? CompraId,
    int? VentaId,
    int? ConsumoInsumoId);

public interface IMovimientoInventarioRepository
{
    Task AddAsync(MovimientoInventario movimiento);
    Task AddConOrigenTipadoAsync(MovimientoInventario movimiento, OrigenMovimientoInventario origen);
    Task<List<MovimientoInventario>> GetByProductoAsync(int productoId);
    Task<List<MovimientoInventario>> GetFilteredAsync(int? productoId, string? tipo, DateTime? desde, DateTime? hasta);
    Task<IReadOnlyDictionary<int, MovimientoInventarioOrigenPersistido>> GetOrigenesTipadosAsync(
        IReadOnlyCollection<int> movimientoIds);
    Task<int?> GetUltimoMovimientoOriginalCompraIdAsync(int compraId);
    Task<bool> ExisteMovimientoPosteriorAsync(
        int ultimoMovimientoOriginalId,
        IReadOnlyCollection<int> productoIds);
}
