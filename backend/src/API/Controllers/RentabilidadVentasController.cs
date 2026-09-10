using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("ventas/rentabilidad")]
public sealed class RentabilidadVentasController : ControllerBase
{
    private readonly IRentabilidadVentasService _service;

    // AppDbContext and IUsuarioScopeService are already canonical scoped services.
    // This bounded construction keeps the N5.4.D service independently testable while
    // avoiding any expansion of authorization scope.
    public RentabilidadVentasController(AppDbContext context, IUsuarioScopeService usuarioScope)
        : this(new RentabilidadVentasService(context, usuarioScope))
    {
    }

    internal RentabilidadVentasController(IRentabilidadVentasService service)
    {
        _service = service;
    }

    [HttpGet("vendedores")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public Task<IActionResult> GetVendedores([FromQuery] ReporteVentasFiltroDto filtro, CancellationToken cancellationToken = default) =>
        ObtenerAsync(filtro, RentabilidadAgrupacion.Vendedor, cancellationToken);

    [HttpGet("clientes")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public Task<IActionResult> GetClientes([FromQuery] ReporteVentasFiltroDto filtro, CancellationToken cancellationToken = default) =>
        ObtenerAsync(filtro, RentabilidadAgrupacion.Cliente, cancellationToken);

    [HttpGet("productos")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public Task<IActionResult> GetProductos([FromQuery] ReporteVentasFiltroDto filtro, CancellationToken cancellationToken = default) =>
        ObtenerAsync(filtro, RentabilidadAgrupacion.Producto, cancellationToken);

    [HttpGet("categorias")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public Task<IActionResult> GetCategorias([FromQuery] ReporteVentasFiltroDto filtro, CancellationToken cancellationToken = default) =>
        ObtenerAsync(filtro, RentabilidadAgrupacion.Categoria, cancellationToken);

    private async Task<IActionResult> ObtenerAsync(
        ReporteVentasFiltroDto filtro,
        RentabilidadAgrupacion agrupacion,
        CancellationToken cancellationToken)
    {
        var errores = ReporteVentasQueryRules.Validate(filtro);
        if (errores.Count > 0)
            return BadRequest(ApiResponse<object>.Fail(string.Join(" ", errores)));

        var resultado = await _service.ObtenerAsync(filtro, agrupacion, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ReporteRentabilidadDto>>.Ok(resultado));
    }
}
