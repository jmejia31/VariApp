using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("dashboard/kpis")]
public sealed class DashboardKpiConfiguracionController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IUsuarioScopeService _usuarioScope;
    private readonly IDashboardService _dashboardService;

    public DashboardKpiConfiguracionController(
        AppDbContext db,
        IUsuarioScopeService usuarioScope,
        IDashboardService dashboardService)
    {
        _db = db;
        _usuarioScope = usuarioScope;
        _dashboardService = dashboardService;
    }

    [HttpGet("configuracion")]
    public async Task<ActionResult<IReadOnlyList<DashboardKpiConfiguracionDto>>> GetConfiguracionAsync(
        CancellationToken cancellationToken)
    {
        var scope = await ResolverScopeAsync();
        if (scope is null)
        {
            return Forbid();
        }

        var configuracionUsuario = await _db.DashboardKpiConfiguraciones
            .AsNoTracking()
            .Where(x => x.UsuarioId == scope.UsuarioId && x.RolId == null)
            .OrderBy(x => x.Orden)
            .ThenBy(x => x.MetricKey)
            .Select(x => new DashboardKpiConfiguracionDto
            {
                MetricKey = x.MetricKey,
                Habilitado = x.Habilitado,
                Orden = x.Orden,
                EtiquetaVisible = x.EtiquetaVisible
            })
            .ToListAsync(cancellationToken);

        if (configuracionUsuario.Count > 0)
        {
            return Ok(configuracionUsuario);
        }

        var configuracionRol = await _db.DashboardKpiConfiguraciones
            .AsNoTracking()
            .Where(x => x.UsuarioId == null && x.RolId == scope.RolId)
            .OrderBy(x => x.Orden)
            .ThenBy(x => x.MetricKey)
            .Select(x => new DashboardKpiConfiguracionDto
            {
                MetricKey = x.MetricKey,
                Habilitado = x.Habilitado,
                Orden = x.Orden,
                EtiquetaVisible = x.EtiquetaVisible
            })
            .ToListAsync(cancellationToken);

        return Ok(configuracionRol);
    }

    [HttpPut("configuracion")]
    public async Task<ActionResult<IReadOnlyList<DashboardKpiConfiguracionDto>>> PutConfiguracionAsync(
        [FromBody] IReadOnlyList<DashboardKpiConfiguracionDto>? configuracion,
        CancellationToken cancellationToken)
    {
        var scope = await ResolverScopeAsync();
        if (scope is null)
        {
            return Forbid();
        }

        if (configuracion is null)
        {
            return BadRequest(new { message = "La configuración es obligatoria." });
        }

        var duplicate = configuracion
            .GroupBy(x => x.MetricKey, StringComparer.Ordinal)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            return BadRequest(new { message = $"MetricKey duplicada: {duplicate.Key}." });
        }

        foreach (var item in configuracion)
        {
            if (!DashboardKpiMetricKeys.IsSupported(item.MetricKey))
            {
                return BadRequest(new { message = $"MetricKey no soportada: {item.MetricKey}." });
            }

            if (item.Orden < 0)
            {
                return BadRequest(new { message = $"Orden inválido para {item.MetricKey}." });
            }
        }

        var existentes = await _db.DashboardKpiConfiguraciones
            .Where(x => x.UsuarioId == scope.UsuarioId && x.RolId == null)
            .ToListAsync(cancellationToken);

        var solicitadas = configuracion.ToDictionary(x => x.MetricKey, StringComparer.Ordinal);

        foreach (var existente in existentes)
        {
            if (!solicitadas.ContainsKey(existente.MetricKey))
            {
                _db.DashboardKpiConfiguraciones.Remove(existente);
            }
        }

        foreach (var item in configuracion)
        {
            var existente = existentes.FirstOrDefault(x => x.MetricKey == item.MetricKey);
            if (existente is null)
            {
                _db.DashboardKpiConfiguraciones.Add(new DashboardKpiConfiguracion
                {
                    MetricKey = item.MetricKey,
                    UsuarioId = scope.UsuarioId,
                    RolId = null,
                    Habilitado = item.Habilitado,
                    Orden = item.Orden,
                    EtiquetaVisible = NormalizarEtiqueta(item.EtiquetaVisible)
                });
            }
            else
            {
                existente.Habilitado = item.Habilitado;
                existente.Orden = item.Orden;
                existente.EtiquetaVisible = NormalizarEtiqueta(item.EtiquetaVisible);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var resultado = configuracion
            .OrderBy(x => x.Orden)
            .ThenBy(x => x.MetricKey)
            .Select(x => new DashboardKpiConfiguracionDto
            {
                MetricKey = x.MetricKey,
                Habilitado = x.Habilitado,
                Orden = x.Orden,
                EtiquetaVisible = NormalizarEtiqueta(x.EtiquetaVisible)
            })
            .ToList();

        return Ok(resultado);
    }

    [HttpGet("resueltos")]
    public async Task<ActionResult<IReadOnlyList<object>>> GetKpisResueltosAsync(
        CancellationToken cancellationToken)
    {
        var scope = await ResolverScopeAsync();
        if (scope is null)
        {
            return Forbid();
        }

        var configuracionResult = await GetConfiguracionAsync(cancellationToken);
        var configuracion = configuracionResult.Value;
        if (configuracion is null && configuracionResult.Result is OkObjectResult ok)
        {
            configuracion = ok.Value as IReadOnlyList<DashboardKpiConfiguracionDto>;
        }

        configuracion ??= Array.Empty<DashboardKpiConfiguracionDto>();
        var resumen = await _dashboardService.GetResumenAsync();

        var valores = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [DashboardKpiMetricKeys.IngresosMes] = resumen.IngresosDelMes,
            [DashboardKpiMetricKeys.VentasMes] = resumen.VentasDelMes,
            [DashboardKpiMetricKeys.ComprasMes] = resumen.ComprasDelMes,
            [DashboardKpiMetricKeys.TotalProductos] = resumen.TotalProductos,
            [DashboardKpiMetricKeys.TotalUnidades] = resumen.TotalUnidades,
            [DashboardKpiMetricKeys.ValorInventario] = resumen.ValorTotalInventario,
            [DashboardKpiMetricKeys.UtilidadBruta] = resumen.UtilidadBruta,
            [DashboardKpiMetricKeys.BalanceOperativo] = resumen.BalanceOperativo,
            [DashboardKpiMetricKeys.CuentasPorCobrar] = resumen.CuentasPorCobrar,
            [DashboardKpiMetricKeys.CuentasPorPagar] = resumen.CuentasPorPagar,
            [DashboardKpiMetricKeys.ProductosStockBajo] = resumen.ProductosStockBajo.Count
        };

        var result = configuracion
            .Where(x => x.Habilitado)
            .OrderBy(x => x.Orden)
            .ThenBy(x => x.MetricKey)
            .Select(x => (object)new
            {
                x.MetricKey,
                x.Orden,
                x.EtiquetaVisible,
                Valor = valores[x.MetricKey]
            })
            .ToList();

        return Ok(result);
    }

    private async Task<UsuarioScopeActual?> ResolverScopeAsync()
    {
        return await _usuarioScope.ObtenerActualAsync();
    }

    private static string? NormalizarEtiqueta(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= 120 ? trimmed : trimmed[..120];
    }
}
