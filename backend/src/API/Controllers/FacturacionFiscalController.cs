using InventoryApp.API.Filters;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using InventoryApp.Domain.Fiscal;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.API.Controllers;

[ApiController]
[Authorize]
[RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Crear)]
[Route("facturacion-fiscal")]
public sealed class FacturacionFiscalController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IUsuarioScopeService _usuarioScope;
    private readonly DocumentoFiscalEmisionService _service;

    public FacturacionFiscalController(
        AppDbContext db,
        IUsuarioScopeService usuarioScope,
        IEnumerable<IProveedorDocumentoFiscal> proveedores)
    {
        _db = db;
        _usuarioScope = usuarioScope;
        _service = new DocumentoFiscalEmisionService(db, proveedores);
    }

    [HttpPost("emisiones")]
    public async Task<IActionResult> Emitir(
        [FromBody] EmitirDocumentoFiscalRequest request,
        CancellationToken cancellationToken)
    {
        SolicitudEmisionFiscal solicitud;
        try
        {
            var perfil = new PerfilFiscalDocumento(
                request.EmpresaId,
                request.SucursalId,
                request.Jurisdiccion,
                request.Proveedor,
                request.TipoDocumento);

            solicitud = new SolicitudEmisionFiscal(
                perfil,
                request.FacturaId,
                request.ClaveIdempotencia,
                request.HashSnapshot);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "SOLICITUD_FISCAL_INVALIDA", mensaje = ex.Message });
        }

        // EmpresaId nunca se acepta como autoridad sólo porque venga por HTTP.
        // La membresía tenant-aware se relee server-side y falla cerrada.
        var tenant = await _usuarioScope.ObtenerActualAsync(request.EmpresaId, cancellationToken);
        if (tenant is null)
            return Forbid();

        // Factura aún es una entidad legacy sin EmpresaId directo. Para impedir que un
        // caller enlace una factura de otro tenant, se exige que el creador de la venta
        // posea una membresía activa en la misma empresa solicitada. Si el histórico no
        // puede demostrar esa pertenencia, el endpoint falla cerrado.
        var facturaPerteneceAlTenant = await (
            from factura in _db.Facturas.AsNoTracking()
            join venta in _db.Ventas.AsNoTracking() on factura.VentaId equals venta.Id
            where factura.Id == solicitud.FacturaId &&
                  venta.CreadoPorUsuarioId.HasValue &&
                  _db.UsuarioEmpresas.Any(membresia =>
                      membresia.UsuarioId == venta.CreadoPorUsuarioId.Value &&
                      membresia.EmpresaId == request.EmpresaId &&
                      membresia.Activa)
            select factura.Id)
            .AnyAsync(cancellationToken);

        if (!facturaPerteneceAlTenant)
        {
            return NotFound(new
            {
                error = "FACTURA_NO_DISPONIBLE_EN_TENANT",
                mensaje = "La factura no existe o no puede demostrarse que pertenece a la empresa solicitada."
            });
        }

        try
        {
            var resultado = await _service.EmitirAsync(solicitud, cancellationToken);
            var body = new DocumentoFiscalEmisionResponse(
                resultado.RegistroId,
                resultado.Estado.ToString(),
                resultado.ReferenciaExterna,
                resultado.CodigoProveedor,
                resultado.Mensaje,
                resultado.EsTransitorio,
                resultado.Idempotente,
                resultado.ReintentoAceptado);

            return resultado.Estado switch
            {
                EstadoResultadoFiscal.Confirmado => Ok(body),
                EstadoResultadoFiscal.Rechazado => UnprocessableEntity(body),
                _ => Accepted(body)
            };
        }
        catch (DocumentoFiscalIdempotenciaException ex)
        {
            return Conflict(new
            {
                error = "IDEMPOTENCY_KEY_REUTILIZADA",
                mensaje = ex.Message
            });
        }
        catch (DocumentoFiscalProveedorNoDisponibleException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "PROVEEDOR_FISCAL_NO_DISPONIBLE",
                mensaje = ex.Message,
                registroId = ex.RegistroId
            });
        }
    }
}

public sealed record EmitirDocumentoFiscalRequest(
    int EmpresaId,
    int? SucursalId,
    int FacturaId,
    string Jurisdiccion,
    string Proveedor,
    string TipoDocumento,
    string ClaveIdempotencia,
    string HashSnapshot);

public sealed record DocumentoFiscalEmisionResponse(
    int RegistroId,
    string Estado,
    string? ReferenciaExterna,
    string? CodigoProveedor,
    string? Mensaje,
    bool EsTransitorio,
    bool Idempotente,
    bool ReintentoAceptado);
