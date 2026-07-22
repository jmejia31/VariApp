using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IFacturaCompartirService
{
    /// Genera un enlace público temporal nuevo. Por seguridad, los enlaces
    /// anteriores de la misma factura quedan expirados automáticamente.
    Task<EnlaceCompartirDto> PrepararCompartirAsync(int facturaId);

    Task RegistrarIntentoAsync(int facturaId, RegistrarEnvioDto dto);

    Task<List<HistorialEnvioDto>> GetHistorialAsync(int facturaId);

    /// Revoca todos los enlaces públicos todavía vigentes de la factura.
    Task<int> RevocarEnlacesAsync(int facturaId);

    /// Sirve el mismo PDF oficial mediante un token válido, no expirado y con
    /// límite de accesos. El token se recibe en claro, pero solo su hash se
    /// compara contra la base de datos.
    Task<(byte[] Pdf, string NombreArchivo)?> ObtenerPdfPorTokenAsync(string token);

    /// Envía exactamente el mismo PDF oficial como adjunto de correo.
    Task<(bool Exito, string Mensaje)> EnviarPorCorreoAsync(int facturaId, string destinatario);
}