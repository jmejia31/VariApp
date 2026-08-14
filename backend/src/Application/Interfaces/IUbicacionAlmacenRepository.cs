using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Interfaces;

public interface IUbicacionAlmacenRepository
{
    Task<UbicacionAlmacen?> GetByIdAsync(int id);
    Task<(List<UbicacionAlmacen> Items, int Total)> BuscarAsync(
        string? termino,
        int? almacenId,
        int? ubicacionPadreId,
        TipoUbicacionAlmacen? tipo,
        bool? activa,
        int pagina,
        int tamanoPagina);
    Task<List<UbicacionAlmacen>> GetActivasAsync(int almacenId);
    Task<bool> ExisteCodigoActivoAsync(int almacenId, string codigo, int? excluirId = null);
    Task<bool> ExisteEnAlmacenAsync(int ubicacionId, int almacenId);
    Task AddAsync(UbicacionAlmacen ubicacion);
    void Update(UbicacionAlmacen ubicacion);
    Task<bool> SaveChangesAsync();
}
