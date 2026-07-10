using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IMovimientoInventarioService
{
    Task<List<MovimientoInventarioDto>> GetFilteredAsync(int? productoId, string? tipo, DateTime? desde, DateTime? hasta);
}
