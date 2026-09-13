using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

/// <summary>
/// Casos de uso tenant-aware para Suscripciones SaaS. La ruta empresaId nunca se
/// usa como autoridad: toda operación deriva el tenant efectivo de IUsuarioScopeService.
/// </summary>
public sealed class SuscripcionesSaaSService : ISuscripcionesSaaSService
{
    private const int IdempotencyKeyMaxLength = 160;
    private readonly ISuscripcionesSaaSRepository _repository;
    private readonly IUsuarioScopeService _usuarioScopeService;

    public SuscripcionesSaaSService(
        ISuscripcionesSaaSRepository repository,
        IUsuarioScopeService usuarioScopeService)
    {
        _repository = repository;
        _usuarioScopeService = usuarioScopeService;
    }

    public async Task<SuscripcionSaaSDto> OnboardingAsync(
        int empresaId,
        OnboardingSuscripcionSaaSRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var scope = await ResolverTenantAsync(empresaId, cancellationToken);
        var key = NormalizarIdempotencyKey(idempotencyKey);
        var codigoPlan = NormalizarCodigoPlan(request.PlanCodigo);
        var inicioUtc = NormalizarUtc(request.InicioUtc);

        var replay = await _repository.ObtenerPorIdempotenciaAsync(
            scope.EmpresaId,
            key,
            cancellationToken);

        if (replay is not null)
            return await ResolverReplayAsync(replay, codigoPlan, inicioUtc, cancellationToken);

        var vigente = await _repository.ObtenerVigenteAsync(
            scope.EmpresaId,
            inicioUtc,
            cancellationToken);
        if (vigente is not null)
        {
            throw new ConflictException(
                $"{SuscripcionSaaSErrorCodes.IdempotencyKeyConflictiva}: el tenant ya posee una suscripción vigente para el instante solicitado.");
        }

        var plan = await _repository.ObtenerPlanActivoPorCodigoAsync(codigoPlan, cancellationToken)
            ?? throw new ResourceNotFoundException(
                $"{SuscripcionSaaSErrorCodes.PlanNoEncontrado}: no existe un plan activo con código '{codigoPlan}'.");

        var suscripcion = new Suscripcion(scope.EmpresaId, plan.Id, inicioUtc);
        await _repository.AgregarAsync(suscripcion, key, cancellationToken);

        try
        {
            await _repository.GuardarCambiosAsync(cancellationToken);
            return Mapear(suscripcion, plan);
        }
        catch (IdempotencyConcurrencyException)
        {
            // Otra solicitud ganó la UNIQUE(EmpresaId, IdempotencyKey) después del
            // primer read. El ledger ganador es ahora la única autoridad: payload
            // equivalente => replay; payload distinto => conflicto determinista.
            var ganador = await _repository.ObtenerPorIdempotenciaAsync(
                scope.EmpresaId,
                key,
                cancellationToken);

            if (ganador is null)
            {
                // No degradar una colisión real a un éxito sin evidencia durable.
                // Si el ganador aún no es observable, propagar el fallo causal.
                throw;
            }

            return await ResolverReplayAsync(ganador, codigoPlan, inicioUtc, cancellationToken);
        }
    }

    public async Task<SuscripcionSaaSDto> ObtenerActualAsync(
        int empresaId,
        DateTime? instanteUtc = null,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolverTenantAsync(empresaId, cancellationToken);
        var instante = NormalizarUtc(instanteUtc ?? DateTime.UtcNow);
        var suscripcion = await _repository.ObtenerVigenteAsync(
            scope.EmpresaId,
            instante,
            cancellationToken)
            ?? throw new ResourceNotFoundException(
                $"{SuscripcionSaaSErrorCodes.SuscripcionNoEncontrada}: el tenant no posee una suscripción vigente.");

        var plan = await ObtenerPlanPersistidoAsync(suscripcion.PlanId, cancellationToken);
        return Mapear(suscripcion, plan);
    }

    public async Task<PaginaSuscripcionSaaSDto<LimiteSuscripcionSaaSDto>> ObtenerLimitesAsync(
        int empresaId,
        LimitesSuscripcionSaaSQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var normalizada = query.Normalizada();
        var scope = await ResolverTenantAsync(empresaId, cancellationToken);
        var suscripcion = await _repository.ObtenerVigenteAsync(
            scope.EmpresaId,
            DateTime.UtcNow,
            cancellationToken)
            ?? throw new ResourceNotFoundException(
                $"{SuscripcionSaaSErrorCodes.SuscripcionNoEncontrada}: el tenant no posee una suscripción vigente.");

        var plan = await ObtenerPlanPersistidoAsync(suscripcion.PlanId, cancellationToken);
        var consulta = plan.Limites
            .OrderBy(x => x.Clave)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(normalizada.Clave))
            consulta = consulta.Where(x => x.Clave == normalizada.Clave);

        var materializada = consulta.ToList();
        var total = materializada.Count;
        var items = materializada
            .Skip((normalizada.Pagina - 1) * normalizada.TamanoPagina)
            .Take(normalizada.TamanoPagina)
            .Select(x => new LimiteSuscripcionSaaSDto(x.Clave, x.ValorMaximo))
            .ToList();

        return new PaginaSuscripcionSaaSDto<LimiteSuscripcionSaaSDto>(
            items,
            normalizada.Pagina,
            normalizada.TamanoPagina,
            total);
    }

    private async Task<SuscripcionSaaSDto> ResolverReplayAsync(
        Suscripcion replay,
        string codigoPlan,
        DateTime inicioUtc,
        CancellationToken cancellationToken)
    {
        var replayPlan = await ObtenerPlanPersistidoAsync(replay.PlanId, cancellationToken);
        if (!string.Equals(replayPlan.Codigo, codigoPlan, StringComparison.Ordinal) ||
            replay.InicioUtc != inicioUtc)
        {
            throw new ConflictException(
                $"{SuscripcionSaaSErrorCodes.IdempotencyKeyConflictiva}: la Idempotency-Key ya fue usada con otro payload.");
        }

        return Mapear(replay, replayPlan);
    }

    private async Task<UsuarioTenantScopeActual> ResolverTenantAsync(
        int empresaId,
        CancellationToken cancellationToken)
    {
        if (empresaId <= 0)
        {
            throw new ForbiddenAccessException(
                $"{SuscripcionSaaSErrorCodes.TenantNoAutorizado}: tenant inválido.");
        }

        var scope = await _usuarioScopeService.ObtenerActualAsync(empresaId, cancellationToken);
        if (scope is null || scope.EmpresaId != empresaId)
        {
            throw new ForbiddenAccessException(
                $"{SuscripcionSaaSErrorCodes.TenantNoAutorizado}: el usuario no posee membresía activa en el tenant solicitado.");
        }

        return scope;
    }

    private async Task<Plan> ObtenerPlanPersistidoAsync(
        int planId,
        CancellationToken cancellationToken)
    {
        return await _repository.ObtenerPlanPorIdAsync(planId, cancellationToken)
            ?? throw new ResourceNotFoundException(
                $"{SuscripcionSaaSErrorCodes.PlanNoEncontrado}: el plan asociado a la suscripción no existe.");
    }

    private static SuscripcionSaaSDto Mapear(Suscripcion suscripcion, Plan plan) =>
        new(
            suscripcion.Id,
            plan.Codigo,
            plan.Nombre,
            suscripcion.Estado,
            suscripcion.InicioUtc,
            suscripcion.FinUtc);

    private static string NormalizarCodigoPlan(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("El código del plan es obligatorio.", nameof(codigo));

        return codigo.Trim().ToUpperInvariant();
    }

    private static string NormalizarIdempotencyKey(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency-Key es obligatorio.", nameof(idempotencyKey));

        var key = idempotencyKey.Trim();
        if (key.Length > IdempotencyKeyMaxLength)
            throw new ArgumentException($"Idempotency-Key no puede superar {IdempotencyKeyMaxLength} caracteres.", nameof(idempotencyKey));

        return key;
    }

    private static DateTime NormalizarUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
