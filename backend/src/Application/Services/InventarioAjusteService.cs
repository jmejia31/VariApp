using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;

namespace InventoryApp.Application.Services;

/// <summary>
/// Adaptador de compatibilidad para los endpoints legacy de ajuste directo.
/// La única autoridad que materializa cambios de stock es IAjusteInventarioService.
/// </summary>
public sealed class InventarioAjusteService : IInventarioAjusteService
{
    private readonly IAjusteInventarioService _ajustes;

    public InventarioAjusteService(IAjusteInventarioService ajustes)
    {
        _ajustes = ajustes;
    }

    public Task<AjusteStockResultadoDto> AjustarProductoAsync(
        int productoId,
        AjusteStockRequest request) =>
        AjustarAsync(productoId, null, request);

    public Task<AjusteStockResultadoDto> AjustarVarianteAsync(
        int productoId,
        int varianteId,
        AjusteStockRequest request) =>
        AjustarAsync(productoId, varianteId, request);

    private async Task<AjusteStockResultadoDto> AjustarAsync(
        int productoId,
        int? varianteId,
        AjusteStockRequest request)
    {
        if (productoId <= 0 || varianteId <= 0)
            throw new BusinessRuleException("El producto o la variante indicada no es válida.");
        if (request.CantidadActualEsperada < 0 || request.CantidadNueva < 0)
            throw new BusinessRuleException("Las cantidades de inventario no pueden ser negativas.");
        if (string.IsNullOrWhiteSpace(request.Motivo))
            throw new BusinessRuleException("El motivo del ajuste de inventario es obligatorio.");
        if (request.CantidadActualEsperada == request.CantidadNueva)
            throw new BusinessRuleException("La nueva cantidad debe ser diferente del stock actual.");

        var motivo = request.Motivo.Trim();
        var borrador = await _ajustes.CreateAsync(new CreateAjusteInventarioDto
        {
            Motivo = motivo,
            Observaciones =
                $"Compatibilidad endpoint legacy. Stock esperado por cliente: {request.CantidadActualEsperada}.",
            Detalles =
            {
                new AjusteInventarioDetalleInputDto
                {
                    ProductoId = productoId,
                    ProductoVarianteId = varianteId,
                    CantidadObjetivo = request.CantidadNueva
                }
            }
        });

        var confirmado = await _ajustes.ConfirmarAsync(borrador.Id)
            ?? throw new InvalidOperationException(
                "No se pudo recuperar el ajuste formal durante la confirmación del endpoint legacy.");

        var detalle = confirmado.Detalles.SingleOrDefault(d =>
            d.ProductoId == productoId && d.ProductoVarianteId == varianteId)
            ?? throw new InvalidOperationException(
                "El ajuste formal confirmado no contiene el detalle de inventario solicitado.");

        var cantidadAnterior = detalle.CantidadAnteriorSnapshot
            ?? throw new InvalidOperationException(
                "El ajuste formal confirmado no materializó el snapshot de stock anterior.");
        var cantidadNueva = detalle.CantidadNuevaSnapshot
            ?? throw new InvalidOperationException(
                "El ajuste formal confirmado no materializó el snapshot de stock nuevo.");

        return new AjusteStockResultadoDto
        {
            ProductoId = productoId,
            ProductoVarianteId = varianteId,
            CantidadAnterior = cantidadAnterior,
            CantidadNueva = cantidadNueva,
            Diferencia = detalle.DiferenciaSnapshot ?? cantidadNueva - cantidadAnterior,
            Motivo = motivo
        };
    }
}
