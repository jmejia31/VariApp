using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Services;

public sealed class ReporteComprasService : IReporteComprasService
{
    private readonly AppDbContext _context;

    public ReporteComprasService(AppDbContext context) => _context = context;

    public async Task<PagedResult<ReporteComprasDetalleDto>> ObtenerDetallePaginadoAsync(
        ReporteComprasFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        Validar(filtro);

        var query = _context.Set<OrdenCompraDetalle>().AsNoTracking();

        if (filtro.DesdeUtc.HasValue)
            query = query.Where(x => x.OrdenCompra.FechaCreacion >= filtro.DesdeUtc.Value);
        if (filtro.HastaUtc.HasValue)
            query = query.Where(x => x.OrdenCompra.FechaCreacion < filtro.HastaUtc.Value);
        if (filtro.ProveedorId.HasValue)
            query = query.Where(x => x.OrdenCompra.ProveedorId == filtro.ProveedorId.Value);
        if (filtro.ProductoId.HasValue)
            query = query.Where(x => x.ProductoId == filtro.ProductoId.Value);
        if (filtro.ProductoVarianteId.HasValue)
            query = query.Where(x => x.ProductoVarianteId == filtro.ProductoVarianteId.Value);
        if (filtro.EstadoOrden.HasValue)
            query = query.Where(x => x.OrdenCompra.Estado == filtro.EstadoOrden.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var baseItems = await query
            .OrderByDescending(x => x.OrdenCompra.FechaCreacion)
            .ThenByDescending(x => x.Id)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .Select(x => new LineaBase
            {
                OrdenCompraId = x.OrdenCompraId,
                OrdenCompraDetalleId = x.Id,
                NumeroOrden = x.OrdenCompra.NumeroOrden,
                FechaCreacionUtc = x.OrdenCompra.FechaCreacion,
                FechaEsperadaUtc = x.OrdenCompra.FechaEsperadaUtc,
                ProveedorId = x.OrdenCompra.ProveedorId,
                ProveedorNombre = x.OrdenCompra.ProveedorNombreSnapshot,
                Moneda = x.OrdenCompra.Moneda,
                ProductoId = x.ProductoId,
                ProductoVarianteId = x.ProductoVarianteId,
                ProductoSku = x.ProductoSkuSnapshot,
                ProductoNombre = x.ProductoNombreSnapshot,
                ProductoMarca = x.ProductoMarcaSnapshot,
                ProductoModelo = x.ProductoModeloSnapshot,
                ProductoColor = x.ProductoColorSnapshot,
                ProductoTalla = x.ProductoTallaSnapshot,
                CantidadOrdenada = x.CantidadOrdenada,
                PrecioUnitarioOrdenado = x.PrecioUnitario
            })
            .ToListAsync(cancellationToken);

        var detalleIds = baseItems.Select(x => x.OrdenCompraDetalleId).ToArray();
        var ordenIds = baseItems.Select(x => x.OrdenCompraId).Distinct().ToArray();

        var facturas = await _context.Set<FacturaProveedorDetalle>()
            .AsNoTracking()
            .Where(x => detalleIds.Contains(x.OrdenCompraDetalleId) && x.FacturaProveedor.Estado != EstadoFacturaProveedor.Anulada)
            .Select(x => new
            {
                x.OrdenCompraDetalleId,
                x.PrecioUnitarioSnapshot,
                FacturaMoneda = x.FacturaProveedor.Moneda
            })
            .ToListAsync(cancellationToken);

        var recepciones = await _context.Set<RecepcionCompraDetalle>()
            .AsNoTracking()
            .Where(x => detalleIds.Contains(x.OrdenCompraDetalleId))
            .GroupBy(x => x.OrdenCompraDetalleId)
            .Select(g => new
            {
                OrdenCompraDetalleId = g.Key,
                Recibida = g.Sum(x => x.CantidadRecibida),
                Aceptada = g.Sum(x => x.CantidadAceptada),
                Danada = g.Sum(x => x.CantidadDanada),
                Faltante = g.Sum(x => x.CantidadFaltante),
                Sobrante = g.Sum(x => x.CantidadSobrante)
            })
            .ToListAsync(cancellationToken);

        var devoluciones = await _context.Set<DevolucionProveedorDetalle>()
            .AsNoTracking()
            .Where(x => detalleIds.Contains(x.OrdenCompraDetalleId) &&
                        _context.Set<DevolucionProveedor>().Any(d =>
                            d.Id == x.DevolucionProveedorId && d.Estado != EstadoDevolucionProveedor.Anulada))
            .GroupBy(x => x.OrdenCompraDetalleId)
            .Select(g => new { OrdenCompraDetalleId = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
            .ToListAsync(cancellationToken);

        var evaluaciones = await _context.Set<EvaluacionProveedor>()
            .AsNoTracking()
            .Where(x => ordenIds.Contains(x.OrdenCompraId))
            .OrderByDescending(x => x.FechaRecepcionUtc)
            .Select(x => new
            {
                x.OrdenCompraId,
                x.FechaEsperadaUtc,
                x.FechaRecepcionUtc,
                x.CantidadOrdenada,
                x.CantidadAceptada,
                x.CantidadDanada,
                x.CantidadSobrante
            })
            .ToListAsync(cancellationToken);

        var facturaPorLinea = facturas.GroupBy(x => x.OrdenCompraDetalleId).ToDictionary(g => g.Key, g => g.First());
        var recepcionPorLinea = recepciones.ToDictionary(x => x.OrdenCompraDetalleId);
        var devolucionPorLinea = devoluciones.ToDictionary(x => x.OrdenCompraDetalleId);
        var evaluacionPorOrden = evaluaciones.GroupBy(x => x.OrdenCompraId).ToDictionary(g => g.Key, g => g.First());

        var items = baseItems.Select(x =>
        {
            facturaPorLinea.TryGetValue(x.OrdenCompraDetalleId, out var factura);
            recepcionPorLinea.TryGetValue(x.OrdenCompraDetalleId, out var recepcion);
            devolucionPorLinea.TryGetValue(x.OrdenCompraDetalleId, out var devolucion);
            evaluacionPorOrden.TryGetValue(x.OrdenCompraId, out var evaluacion);

            var comparable = factura is not null && string.Equals(x.Moneda, factura.FacturaMoneda, StringComparison.OrdinalIgnoreCase);
            var precioFacturado = comparable ? factura!.PrecioUnitarioSnapshot : (decimal?)null;

            return new ReporteComprasDetalleDto
            {
                OrdenCompraId = x.OrdenCompraId,
                OrdenCompraDetalleId = x.OrdenCompraDetalleId,
                NumeroOrden = x.NumeroOrden,
                FechaCreacionUtc = x.FechaCreacionUtc,
                FechaEsperadaUtc = x.FechaEsperadaUtc,
                ProveedorId = x.ProveedorId,
                ProveedorNombre = x.ProveedorNombre,
                Moneda = x.Moneda,
                ProductoId = x.ProductoId,
                ProductoVarianteId = x.ProductoVarianteId,
                ProductoSku = x.ProductoSku,
                ProductoNombre = x.ProductoNombre,
                ProductoMarca = x.ProductoMarca,
                ProductoModelo = x.ProductoModelo,
                ProductoColor = x.ProductoColor,
                ProductoTalla = x.ProductoTalla,
                CantidadOrdenada = x.CantidadOrdenada,
                PrecioUnitarioOrdenado = x.PrecioUnitarioOrdenado,
                PrecioUnitarioFacturado = precioFacturado,
                VariacionPrecioAbsoluta = precioFacturado.HasValue ? precioFacturado.Value - x.PrecioUnitarioOrdenado : null,
                CantidadRecibida = recepcion?.Recibida ?? 0m,
                CantidadAceptada = recepcion?.Aceptada ?? 0m,
                CantidadDanada = recepcion?.Danada ?? 0m,
                CantidadFaltante = recepcion?.Faltante ?? 0m,
                CantidadSobrante = recepcion?.Sobrante ?? 0m,
                CantidadDevueltaEfectiva = devolucion?.Cantidad ?? 0m,
                FechaRecepcionEvaluadaUtc = evaluacion?.FechaRecepcionUtc,
                DesviacionEntregaDias = evaluacion is null ? null : (evaluacion.FechaRecepcionUtc.Date - evaluacion.FechaEsperadaUtc.Date).Days,
                CantidadEvaluadaOrdenada = evaluacion?.CantidadOrdenada,
                CantidadEvaluadaAceptada = evaluacion?.CantidadAceptada,
                CantidadEvaluadaDanada = evaluacion?.CantidadDanada,
                CantidadEvaluadaSobrante = evaluacion?.CantidadSobrante
            };
        }).ToList();

        return new PagedResult<ReporteComprasDetalleDto>
        {
            Items = items,
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            TotalCount = totalCount
        };
    }

    internal static void Validar(ReporteComprasFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        if (filtro.DesdeUtc.HasValue && filtro.HastaUtc.HasValue && filtro.DesdeUtc.Value > filtro.HastaUtc.Value)
            throw new ArgumentException("DesdeUtc no puede ser posterior a HastaUtc.", nameof(filtro));
        if (filtro.ProveedorId is <= 0 || filtro.ProductoId is <= 0 || filtro.ProductoVarianteId is <= 0)
            throw new ArgumentException("Los identificadores de filtro deben ser positivos.", nameof(filtro));
        if (filtro.Page < 1)
            throw new ArgumentException("Page debe ser mayor o igual a 1.", nameof(filtro));
        if (filtro.PageSize < 1 || filtro.PageSize > 100)
            throw new ArgumentException("PageSize debe estar entre 1 y 100.", nameof(filtro));
    }

    private sealed class LineaBase
    {
        public int OrdenCompraId { get; init; }
        public int OrdenCompraDetalleId { get; init; }
        public string NumeroOrden { get; init; } = string.Empty;
        public DateTime FechaCreacionUtc { get; init; }
        public DateTime? FechaEsperadaUtc { get; init; }
        public int ProveedorId { get; init; }
        public string ProveedorNombre { get; init; } = string.Empty;
        public string Moneda { get; init; } = string.Empty;
        public int ProductoId { get; init; }
        public int? ProductoVarianteId { get; init; }
        public string? ProductoSku { get; init; }
        public string? ProductoNombre { get; init; }
        public string? ProductoMarca { get; init; }
        public string? ProductoModelo { get; init; }
        public string? ProductoColor { get; init; }
        public string? ProductoTalla { get; init; }
        public decimal CantidadOrdenada { get; init; }
        public decimal PrecioUnitarioOrdenado { get; init; }
    }
}
