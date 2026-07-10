using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

public class MovimientoInventarioService : IMovimientoInventarioService
{
    private readonly IMovimientoInventarioRepository _repository;

    public MovimientoInventarioService(IMovimientoInventarioRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<MovimientoInventarioDto>> GetFilteredAsync(int? productoId, string? tipo, DateTime? desde, DateTime? hasta)
    {
        var movimientos = await _repository.GetFilteredAsync(productoId, tipo, desde, hasta);
        return movimientos.Select(ToDto).ToList();
    }

    private static MovimientoInventarioDto ToDto(MovimientoInventario m) => new()
    {
        Id = m.Id,
        ProductoId = m.ProductoId,
        ProductoNombre = m.Producto?.Nombre ?? "(producto eliminado)",
        Tipo = m.Tipo.ToString(),
        Cantidad = m.Cantidad,
        StockAnterior = m.StockAnterior,
        StockNuevo = m.StockNuevo,
        ReferenciaTipo = m.ReferenciaTipo,
        ReferenciaId = m.ReferenciaId,
        Descripcion = m.Descripcion,
        CreadoPorNombreUsuario = m.CreadoPorNombreUsuario,
        Fecha = m.Fecha
    };
}
