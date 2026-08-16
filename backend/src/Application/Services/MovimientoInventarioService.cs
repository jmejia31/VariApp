using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
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
        var origenes = await _repository.GetOrigenesTipadosAsync(movimientos.Select(m => m.Id).ToArray());

        return movimientos
            .Select(m => ToDto(m, origenes.GetValueOrDefault(m.Id)))
            .ToList();
    }

    public async Task<PagedResult<MovimientoInventarioDto>> GetPagedAsync(MovimientoInventarioQueryDto query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Desde.HasValue && query.Hasta.HasValue && query.Desde.Value > query.Hasta.Value)
            throw new BusinessRuleException("La fecha inicial del Kardex no puede ser posterior a la fecha final.");

        var (items, totalCount) = await _repository.GetPagedAsync(query);
        var origenes = await _repository.GetOrigenesTipadosAsync(items.Select(m => m.Id).ToArray());

        return new PagedResult<MovimientoInventarioDto>
        {
            Items = items.Select(m => ToDto(m, origenes.GetValueOrDefault(m.Id))).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    private static MovimientoInventarioDto ToDto(
        MovimientoInventario m,
        MovimientoInventarioOrigenPersistido? origen)
    {
        var origenTipo = origen switch
        {
            { CompraId: not null } => "Compra",
            { VentaId: not null } => "Venta",
            { ConsumoInsumoId: not null } => "ConsumoInsumo",
            { AjusteInventarioId: not null } => "AjusteInventario",
            { TransferenciaInventarioId: not null } => "TransferenciaInventario",
            _ => null
        };

        var origenId = origen?.CompraId ??
                       origen?.VentaId ??
                       origen?.ConsumoInsumoId ??
                       origen?.AjusteInventarioId ??
                       origen?.TransferenciaInventarioId;

        return new MovimientoInventarioDto
        {
            Id = m.Id,
            ProductoId = m.ProductoId,
            ProductoVarianteId = m.ProductoVarianteId,
            AlmacenId = m.AlmacenId,
            UbicacionAlmacenId = m.UbicacionAlmacenId,
            ProductoNombre = m.Producto?.Nombre ?? "(producto eliminado)",
            ProductoColor = m.ProductoColorSnapshot ?? m.ProductoVariante?.Color?.Nombre,
            ProductoSku = m.ProductoSkuSnapshot ?? m.ProductoVariante?.Sku,
            ProductoImagenPrincipalUrl = m.Producto?.ImagenPrincipal?.Url,
            Tipo = m.Tipo.ToString(),
            Causa = m.Causa.ToString(),
            Cantidad = m.Cantidad,
            StockAnterior = m.StockAnterior,
            StockNuevo = m.StockNuevo,
            CostoUnitario = m.CostoUnitario,
            PrecioUnitario = m.PrecioUnitario,
            CorrelationId = m.CorrelationId,
            OrigenTipo = origenTipo,
            OrigenId = origenId,
            CompraId = origen?.CompraId,
            VentaId = origen?.VentaId,
            ConsumoInsumoId = origen?.ConsumoInsumoId,
            AjusteInventarioId = origen?.AjusteInventarioId,
            TransferenciaInventarioId = origen?.TransferenciaInventarioId,
            ReferenciaTipo = m.ReferenciaTipo,
            ReferenciaId = m.ReferenciaId,
            Descripcion = m.Descripcion,
            CreadoPorNombreUsuario = m.CreadoPorNombreUsuario,
            Fecha = m.Fecha
        };
    }
}
