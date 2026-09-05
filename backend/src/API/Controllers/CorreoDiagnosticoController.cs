using InventoryApp.Application.Common;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using InventoryApp.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

/// Diagnóstico seguro del transporte SMTP de Desarrollo. Nunca devuelve usuario,
/// contraseña, certificados ni mensajes técnicos completos del proveedor.
[ApiController]
[Authorize]
[Route("facturas/correo")]
public sealed class CorreoDiagnosticoController : ControllerBase
{
    private readonly IEmailService _emailService;

    public CorreoDiagnosticoController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("probar")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Compartir)]
    public async Task<IActionResult> ProbarConexion(CancellationToken cancellationToken)
    {
        var resultado = await _emailService.ProbarConexionAsync(cancellationToken);
        return Ok(ApiResponse<ResultadoDiagnosticoSmtp>.Ok(resultado, resultado.Mensaje));
    }
}
