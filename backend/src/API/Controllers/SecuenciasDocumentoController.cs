using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[Route("secuencias-documento")]
public sealed class SecuenciasDocumentoController : ControllerBase
{
    private readonly ISecuenciaDocumentoService _service;

    public SecuenciasDocumentoController(ISecuenciaDocumentoService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> Get(
        [FromQuery] int empresaId,
        [FromQuery] int? sucursalId,
        [FromQuery] string tipoDocumento,
        CancellationToken cancellationToken)
    {
        var secuencia = await _service.ObtenerAsync(
            empresaId,
            sucursalId,
            tipoDocumento,
            cancellationToken);

        return Ok(ApiResponse<SecuenciaDocumentoDto>.Ok(secuencia));
    }

    [HttpPost("siguiente")]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> ReservarSiguiente(
        [FromBody] ReservarSecuenciaDocumentoRequest request,
        CancellationToken cancellationToken)
    {
        var reserva = await _service.ReservarSiguienteAsync(request, cancellationToken);
        return Ok(ApiResponse<SecuenciaDocumentoSiguienteDto>.Ok(
            reserva,
            "Número reservado correctamente."));
    }
}
