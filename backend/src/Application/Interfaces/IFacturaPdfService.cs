using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

/// Generación real de PDF (sección 13 del prompt: "no simules el envío
/// automático" — y por extensión, no se simula tampoco la generación misma
/// del documento con una vista HTML imprimible como sustituto final).
public interface IFacturaPdfService
{
    Task<byte[]> GenerarPdfAsync(FacturaDto factura);
}
