using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Mappings;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IProductoRepository _productoRepository;
    private readonly ICompraRepository _compraRepository;
    private readonly IVentaRepository _ventaRepository;
    private readonly IRevisionFinancieraRepository _revisionRepository;
    private readonly IMovimientoFinancieroRepository _movimientoFinancieroRepository;

    public DashboardService(
        IProductoRepository productoRepository,
        ICompraRepository compraRepository,
        IVentaRepository ventaRepository,
        IRevisionFinancieraRepository revisionRepository,
        IMovimientoFinancieroRepository movimientoFinancieroRepository)
    {
        _productoRepository = productoRepository;
        _compraRepository = compraRepository;
        _ventaRepository = ventaRepository;
        _revisionRepository = revisionRepository;
        _movimientoFinancieroRepository = movimientoFinancieroRepository;
    }

    public async Task<DashboardResumenDto> GetResumenAsync()
    {
        var stockBajo = await _productoRepository.GetStockBajoAsync();
        var ultimosProductos = await _productoRepository.GetUltimosAgregadosAsync();
        var ultimasCompras = await _compraRepository.GetUltimasAsync();
        var ultimasVentas = await _ventaRepository.GetUltimasAsync();
        var ultimaRevision = await _revisionRepository.GetUltimaAsync();

        var movimientos = await _movimientoFinancieroRepository.GetFilteredAsync(null, null);
        var noAnulados = movimientos.Where(m => m.Estado != EstadoMovimientoFinanciero.Anulado).ToList();
        var balanceOperativo =
            noAnulados.Where(m => m.Tipo == TipoMovimientoFinanciero.Ingreso).Sum(m => m.Monto) -
            noAnulados.Where(m => m.Tipo == TipoMovimientoFinanciero.Egreso).Sum(m => m.Monto);

        return new DashboardResumenDto
        {
            TotalProductos = await _productoRepository.GetTotalProductosAsync(),
            TotalUnidades = await _productoRepository.GetTotalUnidadesAsync(),
            ValorTotalInventario = await _productoRepository.GetValorTotalCostoAsync(),
            ValorPotencialVenta = await _productoRepository.GetValorTotalPrecioAsync(),
            ProductosStockBajo = stockBajo.Select(ProductoMapper.ToDto).ToList(),
            UltimosAgregados = ultimosProductos.Select(ProductoMapper.ToDto).ToList(),

            ComprasDelMes = await _compraRepository.GetTotalDelMesAsync(),
            VentasDelMes = await _ventaRepository.GetTotalDelMesAsync(),
            UltimasCompras = ultimasCompras.Select(c => new CompraResumenDto
            {
                NumeroCompra = c.NumeroCompra, ProveedorNombre = c.ProveedorNombre,
                Total = c.Total, Estado = c.Estado.ToString(), Fecha = c.Fecha
            }).ToList(),
            UltimasVentas = ultimasVentas.Select(v => new VentaResumenDto
            {
                NumeroVenta = v.NumeroVenta, ClienteNombre = v.ClienteNombre,
                Total = v.Total, Estado = v.Estado.ToString(), Fecha = v.Fecha
            }).ToList(),

            IngresosDelMes = await _ventaRepository.GetIngresosDelMesAsync(),
            UtilidadBruta = await _ventaRepository.GetUtilidadBrutaTotalAsync(),
            CuentasPorCobrar = await _ventaRepository.GetCuentasPorCobrarAsync(),
            CuentasPorPagar = await _compraRepository.GetCuentasPorPagarAsync(),
            BalanceOperativo = balanceOperativo,

            UltimaVentaPor = ultimasVentas.FirstOrDefault()?.ConfirmadoPorNombreUsuario ?? ultimasVentas.FirstOrDefault()?.CreadoPorNombreUsuario,
            UltimaCompraPor = ultimasCompras.FirstOrDefault()?.ConfirmadoPorNombreUsuario ?? ultimasCompras.FirstOrDefault()?.CreadoPorNombreUsuario,
            UltimaRevisionFinancieraPor = ultimaRevision?.RevisadoPorNombreUsuario,
            UltimoProductoRegistradoPor = ultimosProductos.FirstOrDefault()?.CreadoPorNombreUsuario
        };
    }
}
