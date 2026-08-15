using InventoryApp.Domain.Common;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public sealed record MovimientoInventarioOrigenPersistido(
    int MovimientoId,
    int? CompraId,
    int? VentaId,
    int? ConsumoInsumoId,
    int? AjusteInventarioId = null);

public interface IMovimientoInventarioRepository
{
    Task AddAsync(MovimientoInventario movimiento);
    Task AddConOrigenTipadoAsync(MovimientoInventario movimiento, OrigenMovimientoInventario origen);

    async Task AddConOrigenTipadoAsync(
        MovimientoInventario movimiento,
        OrigenMovimientoInventario origen,
        ContextoFisicoMovimientoInventario contexto)
    {
        ArgumentNullException.ThrowIfNull(movimiento);
        ArgumentNullException.ThrowIfNull(origen);
        ArgumentNullException.ThrowIfNull(contexto);

        movimiento.ProductoVarianteId = contexto.ProductoVarianteId;
        movimiento.AlmacenId = contexto.AlmacenId;
        movimiento.UbicacionAlmacenId = contexto.UbicacionAlmacenId;
        movimiento.CorrelationId = contexto.CorrelationId;

        await AddConOrigenTipadoAsync(movimiento, origen);
    }

    Task<List<MovimientoInventario>> GetByProductoAsync(int productoId);
    Task<List<MovimientoInventario>> GetFilteredAsync(int? productoId, string? tipo, DateTime? desde, DateTime? hasta);
    Task<IReadOnlyDictionary<int, MovimientoInventarioOrigenPersistido>> GetOrigenesTipadosAsync(
        IReadOnlyCollection<int> movimientoIds);
    Task<int?> GetUltimoMovimientoOriginalCompraIdAsync(int compraId);
    Task<bool> ExisteMovimientoPosteriorAsync(
        int ultimoMovimientoOriginalId,
        IReadOnlyCollection<int> productoIds);
}
