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
[Route("cargas-masivas")]
public class CargasMasivasController : ControllerBase
{
    private readonly ICargaMasivaService _service;

    public CargasMasivasController(ICargaMasivaService service)
    {
        _service = service;
    }

    [HttpGet("configuracion")]
    [RequierePermiso(ModuloSistema.CargasMasivas, AccionPermiso.Ver)]
    public IActionResult Configuracion() =>
        Ok(ApiResponse<CargaMasivaConfiguracionDto>.Ok(_service.ObtenerConfiguracion()));

    [HttpGet("plantillas/{tipo}")]
    [RequierePermiso(ModuloSistema.CargasMasivas, AccionPermiso.Exportar)]
    public async Task<IActionResult> DescargarPlantilla(TipoCargaMasiva tipo, [FromQuery] string formato = "xlsx")
    {
        var archivo = await _service.DescargarPlantillaAsync(tipo, formato);
        return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.CargasMasivas, AccionPermiso.ConsultarHistorial)]
    public async Task<IActionResult> GetPaged([FromQuery] PagedRequest request)
    {
        var resultado = await _service.GetPagedAsync(request);
        return Ok(ApiResponse<PagedResult<CargaMasivaDto>>.Ok(resultado));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.CargasMasivas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var carga = await _service.GetByIdAsync(id);
        if (carga is null) return NotFound(ApiResponse<object>.Fail("Carga masiva no encontrada."));
        return Ok(ApiResponse<CargaMasivaDetalleDto>.Ok(carga));
    }

    [HttpPost("validar")]
    [RequierePermiso(ModuloSistema.CargasMasivas, AccionPermiso.Crear)]
    [RequestSizeLimit(CargaMasivaArchivoLimites.MaximoRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = CargaMasivaArchivoLimites.MaximoRequestBytes)]
    public async Task<IActionResult> Validar(
        [FromForm] TipoCargaMasiva tipo,
        [FromForm] IFormFile archivo,
        CancellationToken cancellationToken)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("Debes seleccionar un archivo CSV o XLSX."));

        await using var stream = archivo.OpenReadStream();
        var resultado = await _service.ValidarAsync(
            tipo,
            archivo.FileName,
            archivo.ContentType,
            archivo.Length,
            stream,
            cancellationToken);

        return Ok(ApiResponse<CargaMasivaDetalleDto>.Ok(
            resultado,
            resultado.ArchivoReutilizado
                ? "Se recuperó la validación existente de este archivo."
                : "Archivo validado. Revisa la vista previa antes de confirmar."));
    }

    [HttpPost("{id:int}/confirmar")]
    [RequierePermiso(ModuloSistema.CargasMasivas, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Confirmar(int id, CancellationToken cancellationToken)
    {
        var resultado = await _service.ConfirmarAsync(id, cancellationToken);
        return Ok(ApiResponse<CargaMasivaDetalleDto>.Ok(resultado, "Carga confirmada mediante una transacción completa."));
    }

    [HttpGet("{id:int}/errores")]
    [RequierePermiso(ModuloSistema.CargasMasivas, AccionPermiso.Exportar)]
    public async Task<IActionResult> DescargarErrores(int id, [FromQuery] string formato = "xlsx")
    {
        var archivo = await _service.DescargarErroresAsync(id, formato);
        return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }
}

internal static class CargaMasivaArchivoLimites
{
    public const long MaximoRequestBytes = 6L * 1024L * 1024L;
}
