using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

/// <summary>
/// Caso de uso N7.1.D para registrar una intención outbox tenant-bound.
/// La persistencia definitiva pertenece al límite transaccional del caller;
/// este servicio valida autoridad server-side e idempotencia antes de stagear.
/// </summary>
public sealed class MensajeOutboxService : IMensajeOutboxService
{
    private readonly IMensajeOutboxRepository _repository;
    private readonly IUsuarioScopeService _usuarioScopeService;

    public MensajeOutboxService(
        IMensajeOutboxRepository repository,
        IUsuarioScopeService usuarioScopeService)
    {
        _repository = repository;
        _usuarioScopeService = usuarioScopeService;
    }

    public async Task<MensajeOutbox> RegistrarAsync(
        int empresaId,
        RegistrarMensajeOutboxRequest request,
        string claveIdempotencia,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (empresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaId), "La empresa/tenant es obligatoria.");

        if (string.IsNullOrWhiteSpace(claveIdempotencia))
            throw new ArgumentException("Idempotency-Key es obligatoria.", nameof(claveIdempotencia));

        var scope = await _usuarioScopeService.ObtenerActualAsync(empresaId, cancellationToken);
        if (scope is null || scope.EmpresaId != empresaId)
            throw new ForbiddenAccessException("No existe una membresía activa para la empresa solicitada.");

        var claveNormalizada = claveIdempotencia.Trim();
        if (await _repository.ExisteClaveIdempotenciaAsync(empresaId, claveNormalizada, cancellationToken))
            throw new ConflictException("La clave de idempotencia ya fue registrada para esta empresa.");

        var mensaje = MensajeOutbox.Crear(
            empresaId,
            request.TipoEvento,
            request.PayloadJson,
            claveNormalizada,
            request.TipoAgregado,
            request.IdAgregado,
            request.CorrelationId);

        await _repository.AddAsync(mensaje, cancellationToken);
        return mensaje;
    }
}
