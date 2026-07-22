using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Mappings;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IProductoRepository _productoRepository;
    private readonly ICompraRepository _compraRepository;
    private readonly IVentaRepository _ventaRepository;
    private readonly IRevisionFinancieraRepository _revisionRepository;
    private readonly IMovimientoFinancieroRepository _movimientoFinancieroRepository;
    private readonly ICurrentUserService _currentUser;

    public DashboardService(
        IProductoRepository productoRepository,
        ICompraRepository compraRepository,
        IVentaRepository ventaRepository,
        IRevisionFinancieraRepository revisionRepository,
        IMovimientoFinancieroRepository movimientoFinancieroRepository,
        ICurrentUserService currentUser)
    {
        _productoRepository = productoRepository;
        _compraRepository = compraRepository;
        _ventaRepository = ventaRepository;
        _revisionRepository = revisionRepository;
        _movimientoFinancieroRepository = movimientoFinancieroRepository;
        _currentUser = currentUser;
    }

    public async Task<DashboardResumenDto> GetResumenAsync()
    {
        var esAdministrador = _currentUser.EsAdministrador;
        var stockBajo = await _productoRepository.GetStockBajoAsync();
        var ultimosProductos = await _productoRepository.GetUltimosAgregadosAsync();
        var ultimasVentas = await _ventaRepository.GetUltimasAsync();

        // Los repositorios de ventas, compras y movimientos aplican alcance por
        // UsuarioId para cualquier usuario no administrador. Además, el dashboard
        // no expone datos financieros ni auditoría corporativa fuera del rol admin.
        var ultimasCompras = esAdministrador
            ? await _compraRepository.GetUltimasAsync()
            : new List<Domain.Entities.Compra>();

        decimal balanceOperativo = 0;
        IRevisionFinancieraRepository? _ = null;
        var ultimaRevision = esAdministrador ? await _revisionRepository.GetUltimaAsync() : null;
        if (esAdministrador)
        {
            var movimientos = await _movimientoFinancieroRepository.GetFilteredAsync(null, null);
            var noAnulados = movimientos.Where(m => m.Estado != EstadoMovimientoFinanciero.Anulado).ToList();
            balanceOperativo =
                noAnulados.Where(m => m.Tipo == TipoMovimientoFinanciero.Ingreso).Sum(m => m.Monto) -
                noAnulados.Where(m => m.Tipo == TipoMovimientoFinanciero.Egreso).Sum(m => m.Monto);
        }

        return new DashboardResumenDto
        {
            TotalProductos = await _productoRepository.GetTotalProductosAsync(),
            TotalUnidades = await _productoRepository.GetTotalUnidadesAsync(),
            ValorTotalInventario = esAdministrador ? await _productoRepository.GetValorTotalCostoAsync() : 0,
            ValorPotencialVenta = esAdministrador ? await _productoRepository.GetValorTotalPrecioAsync() : 0,
            ProductosStockBajo = stockBajo.Select(ProductoMapper.ToDto).ToList(),
            UltimosAgregados = esAdministrador ? ultimosProductos.Select(ProductoMapper.ToDto).ToList() : new(),

            ComprasDelMes = esAdministrador ? await _compraRepository.GetTotalDelMesAsync() : 0,
            VentasDelMes = await _ventaRepository.GetTotalDelMesAsync(),
            UltimasCompras = ultimasCompras.Select(c => new CompraResumenDto
            {
                NumeroCompra = c.NumeroCompra,
                ProveedorNombre = c.ProveedorNombre,
                Total = c.Total,
                Estado = c.Estado.ToString(),
                Fecha = c.Fecha
            }).ToList(),
            UltimasVentas = ultimasVentas.Select(v => new VentaResumenDto
            {
                NumeroVenta = v.NumeroVenta,
                ClienteNombre = v.ClienteNombre,
                Total = v.Total,
                Estado = v.Estado.ToString(),
                Fecha = v.Fecha
            }).ToList(),

            IngresosDelMes = esAdministrador ? await _ventaRepository.GetIngresosDelMesAsync() : 0,
            UtilidadBruta = esAdministrador ? await _ventaRepository.GetUtilidadBrutaTotalAsync() : 0,
            CuentasPorCobrar = esAdministrador ? await _ventaRepository.GetCuentasPorCobrarAsync() : 0,
            CuentasPorPagar = esAdministrador ? await _compraRepository.GetCuentasPorPagarAsync() : 0,
            BalanceOperativo = balanceOperativo,

            UltimaVentaPor = esAdministrador ? ultimasVentas.FirstOrDefault()?.ConfirmadoPorNombreUsuario ?? ultimasVentas.FirstOrDefault()?.CreadoPorNombreUsuario : null,
            UltimaCompraPor = esAdministrador ? ultimasCompras.FirstOrDefault()?.ConfirmadoPorNombreUsuario ?? ultimasCompras.FirstOrDefault()?.CreadoPorNombreUsuario : null,
            UltimaRevisionFinancieraPor = esAdministrador ? ultimaRevision?.RevisadoPorNombreUsuario : null,
            UltimoProductoRegistradoPor = esAdministrador ? ultimosProductos.FirstOrDefault()?.CreadoPorNombreUsuario : null
        };
    }
}
