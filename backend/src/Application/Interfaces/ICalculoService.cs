using InventoryApp.Application.DTOs;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Interfaces;

public interface ICalculoService
{
    Task<ResultadoCalculoDto> CalcularVentaAsync(
        List<DetalleCalculoInput> detalles,
        int? clienteId,
        int? rolIdUsuario,
        string? codigoPromocional,
        int? costoEnvioId = null,
        bool envioExonerado = false,
        string? motivoExoneracionEnvio = null);

    Task<ResultadoCalculoDto> CalcularCompraAsync(
        List<DetalleCalculoInput> detalles,
        int? proveedorId);

    Task RegistrarUsoVentaAsync(int ventaId, int? clienteId, List<Domain.Entities.VentaDescuento> descuentos, List<Domain.Entities.VentaImpuesto> impuestos);
    Task RegistrarUsoCompraAsync(int compraId, List<Domain.Entities.CompraImpuesto> impuestos);
}
