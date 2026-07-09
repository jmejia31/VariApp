using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Mappings;

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
            ProductosStockBajo = stockBajo.Select(ProductoMapper.ToDto).ToList(),
            UltimosAgregados = ultimos.Select(ProductoMapper.ToDto).ToList()
        };
    }
}
