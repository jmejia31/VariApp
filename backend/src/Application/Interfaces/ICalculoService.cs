using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Interfaces;

/// Motor de cálculo real en backend (sección 13). Nunca confía en descuento/
/// impuesto/total enviado desde Angular: recibe productos+cantidades y un
/// código promocional opcional, y recalcula todo desde el catálogo.
public interface ICalculoService
{
    Task<ResultadoCalculoDto> CalcularVentaAsync(
        List<DetalleCalculoInput> detalles,
        int? clienteId,
        int? rolIdUsuario,
        string? codigoPromocional);

    Task<ResultadoCalculoDto> CalcularCompraAsync(
        List<DetalleCalculoInput> detalles,
        int? proveedorId);

    /// Registra el uso histórico de los descuentos/impuestos que ya quedaron
    /// guardados como snapshot (VentaDescuento/VentaImpuesto) en el documento.
    /// Se llama solo al CONFIRMAR el documento, nunca al solo calcular/crear
    /// un borrador (evita usos "fantasma" de ventas que nunca se confirman).
    Task RegistrarUsoVentaAsync(int ventaId, int? clienteId, List<Domain.Entities.VentaDescuento> descuentos, List<Domain.Entities.VentaImpuesto> impuestos);
    Task RegistrarUsoCompraAsync(int compraId, List<Domain.Entities.CompraImpuesto> impuestos);
}
