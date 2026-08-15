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
        var origenes = await _repository.GetOrigenesTipadosAsync(movimientos.Select(m => m.Id).ToArray());

        return movimientos
            .Select(m => ToDto(m, origenes.GetValueOrDefault(m.Id)))
            .ToList();
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
            _ => null
        };

        var origenId = origen?.CompraId ??
                       origen?.VentaId ??
                       origen?.ConsumoInsumoId ??
                       origen?.AjusteInventarioId;

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
            ReferenciaTipo = m.ReferenciaTipo,
            ReferenciaId = m.ReferenciaId,
            Descripcion = m.Descripcion,
            CreadoPorNombreUsuario = m.CreadoPorNombreUsuario,
            Fecha = m.Fecha
        };
    }
}
