using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InventoryApp.Infrastructure.Services;

/// <summary>
/// Nombre de servicio conservado por compatibilidad con la configuración de
/// inyección existente. La implementación real está centralizada en
/// QuestPdfFacturaPerfilesService.
/// </summary>
public sealed class QuestPdfFacturaService : IFacturaPdfService
{
    private readonly QuestPdfFacturaPerfilesService _inner;

    public QuestPdfFacturaService(
        IConfiguration configuration,
        ILogger<QuestPdfFacturaPerfilesService> logger)
    {
        _inner = new QuestPdfFacturaPerfilesService(configuration, logger);
    }

    public Task<byte[]> GenerarPdfAsync(FacturaDto factura) =>
        _inner.GenerarPdfAsync(factura);

    public Task<byte[]> GenerarPdfAsync(FacturaDto factura, FacturaFormatoPdf formato) =>
        _inner.GenerarPdfAsync(factura, formato);
}
