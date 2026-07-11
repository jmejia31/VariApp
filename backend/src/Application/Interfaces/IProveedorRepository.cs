using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IProveedorRepository
{
    Task<Proveedor?> GetByIdAsync(int id);
    Task<Proveedor?> GetByIdConComprasAsync(int id);
    Task<List<Proveedor>> GetAllAsync();
    Task<List<Proveedor>> GetActivosAsync();
    Task<List<Proveedor>> BuscarActivosAsync(string termino, int limite = 10);
    Task<Proveedor?> BuscarCoincidenciaActivaAsync(string? documento, string? correo, string? telefono, string? nombre);
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null);
    Task AddAsync(Proveedor proveedor);
    void Update(Proveedor proveedor);
    void Remove(Proveedor proveedor);
    Task<bool> SaveChangesAsync();
}
