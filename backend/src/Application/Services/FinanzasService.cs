using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class FinanzasService : IFinanzasService
{
    private readonly IMovimientoFinancieroRepository _movimientoRepository;
    private readonly IRevisionFinancieraRepository _revisionRepository;
    private readonly IVentaRepository _ventaRepository;
    private readonly ICompraRepository _compraRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly ICurrentUserService _currentUser;

    public FinanzasService(
        IMovimientoFinancieroRepository movimientoRepository,
        IRevisionFinancieraRepository revisionRepository,
        IVentaRepository ventaRepository,
        ICompraRepository compraRepository,
        IProductoRepository productoRepository,
        ICurrentUserService currentUser)
    {
        _movimientoRepository = movimientoRepository;
        _revisionRepository = revisionRepository;
        _ventaRepository = ventaRepository;
        _compraRepository = compraRepository;
        _productoRepository = productoRepository;
        _currentUser = currentUser;
    }

    public async Task<FinanzasResumenDto> GetResumenAsync()
    {
        var esAdministrador = _currentUser.EsAdministrador;
        var movimientos = await _movimientoRepository.GetFilteredAsync(null, null);
        var noAnulados = movimientos.Where(m => m.Estado != EstadoMovimientoFinanciero.Anulado).ToList();

        // MovimientoFinancieroRepository limita por UsuarioId a cualquier usuario
        // no administrador. Por ello los totales siguientes nunca mezclan vendedores.
        var ingresosTotales = noAnulados.Where(m => m.Tipo == TipoMovimientoFinanciero.Ingreso).Sum(m => m.Monto);
        var egresosTotales = noAnulados.Where(m => m.Tipo == TipoMovimientoFinanciero.Egreso).Sum(m => m.Monto);

        // Utilidad, costos de inventario, cuentas por pagar y revisiones financieras
        // son datos corporativos y solo se entregan al administrador.
        var utilidadBruta = esAdministrador
            ? await _ventaRepository.GetUtilidadBrutaTotalAsync()
            : 0m;

        var gastosOperativosManuales = esAdministrador
            ? noAnulados.Where(m => !m.EsAutomatico && m.Tipo == TipoMovimientoFinanciero.Egreso).Sum(m => m.Monto)
            : 0m;
        var utilidadNeta = esAdministrador ? utilidadBruta - gastosOperativosManuales : 0m;

        var valorInventarioCosto = esAdministrador
            ? await _productoRepository.GetValorTotalCostoAsync()
            : 0m;
        var valorPotencialVenta = esAdministrador
            ? await _productoRepository.GetValorTotalPrecioAsync()
            : 0m;

        // VentaRepository aplica alcance por UsuarioId para usuarios no administradores.
        var cuentasPorCobrar = await _ventaRepository.GetCuentasPorCobrarAsync();
        var ventasDelMes = await _ventaRepository.GetTotalDelMesAsync();
        var ingresosDelMes = await _ventaRepository.GetIngresosDelMesAsync();

        var cuentasPorPagar = esAdministrador
            ? await _compraRepository.GetCuentasPorPagarAsync()
            : 0m;
        var comprasDelMes = esAdministrador
            ? await _compraRepository.GetTotalDelMesAsync()
            : 0;
        var ultimaRevision = esAdministrador
            ? await _revisionRepository.GetUltimaAsync()
            : null;

        return new FinanzasResumenDto
        {
            IngresosTotales = ingresosTotales,
            EgresosTotales = egresosTotales,
            UtilidadBruta = utilidadBruta,
            UtilidadNeta = utilidadNeta,
            ValorInventarioCosto = valorInventarioCosto,
            ValorPotencialVenta = valorPotencialVenta,
            CuentasPorCobrar = cuentasPorCobrar,
            CuentasPorPagar = cuentasPorPagar,
            BalanceOperativo = ingresosTotales - egresosTotales,
            VentasDelMes = ventasDelMes,
            ComprasDelMes = comprasDelMes,
            IngresosDelMes = ingresosDelMes,
            UltimaRevision = ultimaRevision is null ? null : ToDto(ultimaRevision)
        };
    }

    public async Task<List<MovimientoFinancieroDto>> GetMovimientosAsync(DateTime? desde, DateTime? hasta)
    {
        var movimientos = await _movimientoRepository.GetFilteredAsync(desde, hasta);
        return movimientos.Select(ToDto).ToList();
    }

    public async Task<MovimientoFinancieroDto> RegistrarMovimientoManualAsync(CreateMovimientoManualDto dto)
    {
        if (dto.Monto <= 0)
            throw new BusinessRuleException("El monto debe ser mayor a 0.");
        if (string.IsNullOrWhiteSpace(dto.Concepto))
            throw new BusinessRuleException("El concepto es obligatorio.");

        if (!Enum.TryParse<TipoMovimientoFinanciero>(dto.Tipo, true, out var tipo))
            tipo = TipoMovimientoFinanciero.Egreso;
        if (!Enum.TryParse<CategoriaMovimientoFinanciero>(dto.Categoria, true, out var categoria))
            categoria = CategoriaMovimientoFinanciero.GastoOperativo;

        MetodoPago? metodoPago = null;
        if (!string.IsNullOrWhiteSpace(dto.MetodoPago) && Enum.TryParse<MetodoPago>(dto.MetodoPago, true, out var mp))
            metodoPago = mp;

        var movimiento = new MovimientoFinanciero
        {
            Tipo = tipo,
            Categoria = categoria,
            Concepto = dto.Concepto,
            Descripcion = dto.Descripcion,
            Monto = dto.Monto,
            Estado = EstadoMovimientoFinanciero.Pagado,
            MetodoPago = metodoPago,
            EsAutomatico = false,
            ModuloOrigen = "Manual",
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await _movimientoRepository.AddAsync(movimiento);
        await _movimientoRepository.SaveChangesAsync();

        return ToDto(movimiento);
    }

    public async Task<MovimientoFinancieroDto?> AnularMovimientoAsync(int id, string motivo)
    {
        var movimiento = await _movimientoRepository.GetByIdAsync(id);
        if (movimiento is null) return null;

        if (movimiento.EsAutomatico)
            throw new BusinessRuleException(
                "Los movimientos automáticos no se pueden anular directamente. Anula la compra o venta que los originó.");
        if (movimiento.Estado == EstadoMovimientoFinanciero.Anulado)
            throw new BusinessRuleException("Este movimiento ya está anulado.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessRuleException("El motivo de anulación es obligatorio.");

        movimiento.Estado = EstadoMovimientoFinanciero.Anulado;
        movimiento.AnuladoPorUsuarioId = _currentUser.UsuarioId;
        movimiento.AnuladoPorNombreUsuario = _currentUser.NombreUsuario;
        movimiento.FechaAnulacion = DateTime.UtcNow;
        movimiento.MotivoAnulacion = motivo;

        _movimientoRepository.Update(movimiento);
        await _movimientoRepository.SaveChangesAsync();

        return ToDto(movimiento);
    }

    public async Task<List<RevisionFinancieraDto>> GetRevisionesAsync()
    {
        if (!_currentUser.EsAdministrador)
            return new List<RevisionFinancieraDto>();

        var revisiones = await _revisionRepository.GetAllAsync();
        return revisiones.Select(ToDto).ToList();
    }

    public async Task<RevisionFinancieraDto> RegistrarRevisionAsync(CreateRevisionFinancieraDto dto)
    {
        if (!_currentUser.EsAdministrador)
            throw new BusinessRuleException("Solo un administrador puede registrar revisiones financieras.");

        if (dto.FechaHasta < dto.FechaDesde)
            throw new BusinessRuleException("La fecha 'hasta' no puede ser anterior a la fecha 'desde'.");

        if (!Enum.TryParse<EstadoRevisionFinanciera>(dto.EstadoRevision, true, out var estado))
            estado = EstadoRevisionFinanciera.Revisado;

        var revision = new RevisionFinanciera
        {
            FechaDesde = dto.FechaDesde,
            FechaHasta = dto.FechaHasta,
            EstadoRevision = estado,
            Observaciones = dto.Observaciones,
            RevisadoPorUsuarioId = _currentUser.UsuarioId ?? 0,
            RevisadoPorNombreUsuario = _currentUser.NombreCompleto ?? _currentUser.NombreUsuario ?? "—",
            FechaRevision = DateTime.UtcNow
        };

        await _revisionRepository.AddAsync(revision);
        await _revisionRepository.SaveChangesAsync();

        return ToDto(revision);
    }

    private static MovimientoFinancieroDto ToDto(MovimientoFinanciero m) => new()
    {
        Id = m.Id,
        Fecha = m.Fecha,
        Tipo = m.Tipo.ToString(),
        Categoria = m.Categoria.ToString(),
        Concepto = m.Concepto,
        Descripcion = m.Descripcion,
        Monto = m.Monto,
        Estado = m.Estado.ToString(),
        MetodoPago = m.MetodoPago?.ToString(),
        EsAutomatico = m.EsAutomatico,
        ModuloOrigen = m.ModuloOrigen,
        CreadoPorNombreUsuario = m.CreadoPorNombreUsuario,
        AnuladoPorNombreUsuario = m.AnuladoPorNombreUsuario,
        FechaAnulacion = m.FechaAnulacion,
        MotivoAnulacion = m.MotivoAnulacion
    };

    private static RevisionFinancieraDto ToDto(RevisionFinanciera r) => new()
    {
        Id = r.Id,
        FechaDesde = r.FechaDesde,
        FechaHasta = r.FechaHasta,
        RevisadoPorNombreUsuario = r.RevisadoPorNombreUsuario,
        FechaRevision = r.FechaRevision,
        EstadoRevision = r.EstadoRevision.ToString(),
        Observaciones = r.Observaciones
    };
}
