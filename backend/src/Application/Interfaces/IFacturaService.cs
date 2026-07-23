using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IFacturaService
{
    Task<FacturaDto?> GetByIdAsync(int id);
    Task<FacturaDto?> GetByVentaIdAsync(int ventaId);
    Task<List<FacturaDto>> GetAllAsync();

    /// <summary>
    /// Uso exclusivo del flujo de enlace público después de validar el token.
    /// No debe exponerse directamente desde un controlador.
    /// </summary>
    Task<FacturaDto?> GetByIdParaEnlacePublicoValidadoAsync(int id);
}
