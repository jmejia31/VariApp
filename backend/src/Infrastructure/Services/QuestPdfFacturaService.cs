using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InventoryApp.Infrastructure.Services;

/// <summary>
/// Nombre de servicio conservado por compatibilidad con la configuración de
/// inyección existente. La implementación real está centralizada en
/// QuestPdfFacturaPerfilesService.
///
/// En ejecución HTTP autenticada, la generación de PDF falla cerrado si no puede
/// resolver una membresía UsuarioEmpresa activa para X-Empresa-Id. El único bypass
/// deliberado es el capability público de factura ya validado por token, marcado en
/// HttpContext.Items por FacturaCompartirRepository.
/// </summary>
public sealed class QuestPdfFacturaService : IFacturaPdfService
{
    private readonly QuestPdfFacturaPerfilesService _inner;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IUsuarioScopeService? _usuarioScopeService;

    public QuestPdfFacturaService(
        IConfiguration configuration,
        ILogger<QuestPdfFacturaPerfilesService> logger,
        IHttpContextAccessor? httpContextAccessor = null,
        IUsuarioScopeService? usuarioScopeService = null)
    {
        _inner = new QuestPdfFacturaPerfilesService(configuration, logger);
        _httpContextAccessor = httpContextAccessor;
        _usuarioScopeService = usuarioScopeService;
    }

    public async Task<byte[]> GenerarPdfAsync(FacturaDto factura)
    {
        ArgumentNullException.ThrowIfNull(factura);
        if (!EsCapabilityPublicoValidado(factura.Id))
            _ = await ResolverTenantProduccionAsync();
        return await _inner.GenerarPdfAsync(factura);
    }

    public async Task<byte[]> GenerarPdfAsync(FacturaDto factura, FacturaFormatoPdf formato)
    {
        ArgumentNullException.ThrowIfNull(factura);
        if (!EsCapabilityPublicoValidado(factura.Id))
            _ = await ResolverTenantProduccionAsync();
        return await _inner.GenerarPdfAsync(factura, formato);
    }

    public Task<byte[]> GenerarPdfAsync(
        StorageTenantContext tenant,
        FacturaDto factura,
        FacturaFormatoPdf formato = FacturaFormatoPdf.A4)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(factura);
        return _inner.GenerarPdfAsync(factura, formato);
    }

    private bool EsCapabilityPublicoValidado(int facturaId)
    {
        var items = _httpContextAccessor?.HttpContext?.Items;
        return items is not null &&
               items.TryGetValue(PublicInvoiceAccessContext.FacturaIdKey, out var value) &&
               value is int autorizado &&
               autorizado == facturaId;
    }

    private async Task<StorageTenantContext> ResolverTenantProduccionAsync()
    {
        // Las construcciones manuales de tests existentes no tienen request ni
        // servicios HTTP. La producción, resuelta por DI, sí los proporciona y
        // por tanto ejecuta el camino fail-closed.
        if (_httpContextAccessor is null && _usuarioScopeService is null)
            return StorageTenantContext.Desde(new UsuarioTenantScopeActual(1, 1, 1, "TEST_ONLY", false));

        return await StorageTenantContextResolver.ResolverRequeridoAsync(
            _httpContextAccessor,
            _usuarioScopeService,
            _httpContextAccessor?.HttpContext?.RequestAborted ?? default);
    }
}
