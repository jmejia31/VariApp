using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
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
    private readonly IUsuarioScopeService _usuarioScope;

    public DashboardService(
        IProductoRepository productoRepository,
        ICompraRepository compraRepository,
        IVentaRepository ventaRepository,
        IRevisionFinancieraRepository revisionRepository,
        IMovimientoFinancieroRepository movimientoFinancieroRepository,
        IUsuarioScopeService usuarioScope)
    {
        _productoRepository = productoRepository;
        _compraRepository = compraRepository;
        _ventaRepository = ventaRepository;
        _revisionRepository = revisionRepository;
        _movimientoFinancieroRepository = movimientoFinancieroRepository;
        _usuarioScope = usuarioScope;
    }

    public async Task<DashboardResumenDto> GetResumenAsync()
    {
        var alcance = await _usuarioScope.ObtenerActualAsync()
            ?? throw new ForbiddenAccessException("No fue posible resolver el usuario autenticado y su rol vigente.");
        var esAdministrador = alcance.EsAdministrador;

        // El inventario físico y las alertas de stock son información operativa
        // compartida. Los costos, utilidades, compras y auditoría son corporativos.
        var stockBajo = await _productoRepository.GetStockBajoAsync();
        var ultimasVentas = await _ventaRepository.GetUltimasAsync();
        var ultimosProductos = esAdministrador
            ? await _productoRepository.GetUltimosAgregadosAsync()
            : new List<Producto>();
        var ultimasCompras = esAdministrador
            ? await _compraRepository.GetUltimasAsync()
            : new List<Compra>();

        decimal balanceOperativo = 0;
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
            UltimosAgregados = ultimosProductos.Select(ProductoMapper.ToDto).ToList(),

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

            IngresosDelMes = await _ventaRepository.GetIngresosDelMesAsync(),
            UtilidadBruta = esAdministrador ? await _ventaRepository.GetUtilidadBrutaTotalAsync() : 0,
            CuentasPorCobrar = await _ventaRepository.GetCuentasPorCobrarAsync(),
            CuentasPorPagar = esAdministrador ? await _compraRepository.GetCuentasPorPagarAsync() : 0,
            BalanceOperativo = balanceOperativo,

            UltimaVentaPor = esAdministrador
                ? ultimasVentas.FirstOrDefault()?.ConfirmadoPorNombreUsuario ?? ultimasVentas.FirstOrDefault()?.CreadoPorNombreUsuario
                : null,
            UltimaCompraPor = esAdministrador
                ? ultimasCompras.FirstOrDefault()?.ConfirmadoPorNombreUsuario ?? ultimasCompras.FirstOrDefault()?.CreadoPorNombreUsuario
                : null,
            UltimaRevisionFinancieraPor = esAdministrador ? ultimaRevision?.RevisadoPorNombreUsuario : null,
            UltimoProductoRegistradoPor = esAdministrador ? ultimosProductos.FirstOrDefault()?.CreadoPorNombreUsuario : null
        };
    }
}
