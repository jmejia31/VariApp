using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface ISecuenciaDocumentoService
{
    Task<SecuenciaDocumentoDto> ObtenerAsync(
        int empresaId,
        int? sucursalId,
        string tipoDocumento,
        CancellationToken cancellationToken = default);

    Task<SecuenciaDocumentoSiguienteDto> ReservarSiguienteAsync(
        ReservarSecuenciaDocumentoRequest request,
        CancellationToken cancellationToken = default);
}
