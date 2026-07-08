namespace InventoryApp.Application.DTOs;

public class DashboardResumenDto
{
    public int TotalProductos { get; set; }
    public int TotalUnidades { get; set; }
    public decimal ValorTotalInventario { get; set; } // suma de Costo * Cantidad
    public decimal ValorPotencialVenta { get; set; }  // suma de Precio * Cantidad
    public List<ProductoDto> ProductosStockBajo { get; set; } = new();
    public List<ProductoDto> UltimosAgregados { get; set; } = new();
}
