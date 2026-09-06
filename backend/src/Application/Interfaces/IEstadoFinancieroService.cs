using InventoryApp.Application.DTOs.Contabilidad;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Interfaces;

/// <summary>
/// Contrato de aplicación para generar los estados financieros soportados por N4.10.
/// </summary>
public interface IEstadoFinancieroService
{
    Task<EstadoFinancieroDto> GenerarAsync(
        TipoEstadoFinanciero tipo,
        EstadoFinancieroFiltroDto filtro,
        CancellationToken cancellationToken = default);
}
