using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

/// Genera el PDF oficial de la factura. A4 se conserva como formato
/// predeterminado para correo, WhatsApp y enlaces públicos; descarga e impresión
/// pueden solicitar un perfil explícito sin alterar el snapshot fiscal.
public interface IFacturaPdfService
{
    Task<byte[]> GenerarPdfAsync(FacturaDto factura);

    Task<byte[]> GenerarPdfAsync(FacturaDto factura, FacturaFormatoPdf formato) =>
        GenerarPdfAsync(factura);
}
