using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IFacturaCompartirService
{
    /// Genera (o reutiliza uno vigente) un enlace público temporal al PDF y
    /// arma el mensaje de WhatsApp con la plantilla formal del negocio.
    Task<EnlaceCompartirDto> PrepararCompartirAsync(int facturaId);

    /// Registra el intento de envío (sección 14/18: "registrar el intento").
    Task RegistrarIntentoAsync(int facturaId, RegistrarEnvioDto dto);

    Task<List<HistorialEnvioDto>> GetHistorialAsync(int facturaId);

    /// Usado por el endpoint público (sin autenticación) para servir el PDF
    /// a través de un token válido y no expirado.
    Task<(byte[] Pdf, string NombreArchivo)?> ObtenerPdfPorTokenAsync(string token);
}
