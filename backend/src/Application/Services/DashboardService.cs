using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IProductoRepository _repository;

    public DashboardService(IProductoRepository repository)
    {
        _repository = repository;
    }

    public async Task<DashboardResumenDto> GetResumenAsync()
    {
        var totalProductos = await _repository.GetTotalProductosAsync();
        var totalUnidades = await _repository.GetTotalUnidadesAsync();
        var valorCosto = await _repository.GetValorTotalCostoAsync();
        var valorPrecio = await _repository.GetValorTotalPrecioAsync();
        var stockBajo = await _repository.GetStockBajoAsync();
        var ultimos = await _repository.GetUltimosAgregadosAsync();

        return new DashboardResumenDto
        {
            TotalProductos = totalProductos,
            TotalUnidades = totalUnidades,
            ValorTotalInventario = valorCosto,
            ValorPotencialVenta = valorPrecio,
            ProductosStockBajo = stockBajo.Select(ToDto).ToList(),
            UltimosAgregados = ultimos.Select(ToDto).ToList()
        };
    }

    private static ProductoDto ToDto(Producto producto) => new()
    {
        Id = producto.Id,
        Nombre = producto.Nombre,
        Marca = producto.Marca,
        Modelo = producto.Modelo,
        Descripcion = producto.Descripcion,
        Cantidad = producto.Cantidad,
        Costo = producto.Costo,
        Precio = producto.Precio,
        ImagenUrl = producto.ImagenUrl,
        UmbralStockBajo = producto.UmbralStockBajo,
        TieneStockBajo = producto.TieneStockBajo,
        FechaCreacion = producto.FechaCreacion,
        FechaActualizacion = producto.FechaActualizacion
    };
}
