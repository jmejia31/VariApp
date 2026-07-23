using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IFacturaRepository
{
    Task<Factura?> GetByIdAsync(int id);
    Task<Factura?> GetByVentaIdAsync(int ventaId);
    Task<List<Factura>> GetAllAsync();

    /// <summary>
    /// Recupera la factura sin aplicar el alcance del usuario autenticado.
    /// Solo puede usarse después de validar un enlace público por su hash,
    /// expiración, revocación y límite de accesos.
    /// </summary>
    Task<Factura?> GetByIdParaEnlacePublicoValidadoAsync(int id);

    Task<int> ContarTodasAsync();
    Task AddAsync(Factura factura);
    void Update(Factura factura);
    Task<bool> SaveChangesAsync();
}
