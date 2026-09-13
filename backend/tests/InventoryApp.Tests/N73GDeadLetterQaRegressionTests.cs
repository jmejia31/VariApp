using InventoryApp.Application.Common.Interfaces;
using InventoryApp.Application.Common.Services;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Repositories;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N73GDeadLetterQaRegressionTests
{
    [Fact]
    public async Task QaRegression_Matriz_MultiTenant_Preserva_DeadLetter_Idempotencia_Y_Aislamiento()
    {
        const string sharedKey = "n73g-shared-key";
        var now = new DateTimeOffset(2027, 3, 7, 18, 0, 0, TimeSpan.Zero);

        var tenant73Pending = CrearMensaje(7301, 73, sharedKey, MensajeOutboxEstado.Pending, maxIntentos: 5);
        var tenant74Pending = CrearMensaje(7401, 74, sharedKey, MensajeOutboxEstado.Pending, maxIntentos: 5);
        var tenant73DeadLetter = CrearMensaje(7302, 73, "n73g-dead", MensajeOutboxEstado.DeadLetter, maxIntentos: 2);

        var repository = new FakeRepository(new[] { tenant73Pending, tenant74Pending, tenant73DeadLetter });
        repository.Existing[74, sharedKey] = tenant74Pending;

        var processor = new OutboxRetryProcessor(repository, new FixedClock(now));
        var publicados = new List<long>();

        var procesados = await processor.ProcesarAsync(
            empresaId: 73,
            batchSize: 10,
            publicarAsync: item =>
            {
                publicados.Add(item.Id);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.Equal(1, procesados);
        Assert.Equal(new[] { 7301L }, publicados);
        Assert.Equal(MensajeOutboxEstado.Delivered, tenant73Pending.Estado);
        Assert.Equal(MensajeOutboxEstado.Pending, tenant74Pending.Estado);
        Assert.Equal(MensajeOutboxEstado.DeadLetter, tenant73DeadLetter.Estado);
        Assert.True(tenant73DeadLetter.EstaEnDeadLetter);
        Assert.Equal((short)2, tenant73DeadLetter.Intentos);
        Assert.Single(repository.ClaimTenantIds);
        Assert.Equal(73, repository.ClaimTenantIds[0]);

        var service = new MensajeOutboxService(
            new FixedCurrentUserService(73),
            repository,
            new FixedClock(now.AddMinutes(1)));

        var creation = await service.CrearAsync(new CrearMensajeOutboxRequest
        {
            ClaveIdempotencia = sharedKey,
            Tipo = "n7.3.g-qa-regression",
            PayloadJson = """{"empresaId":74,"source":"qa"}""",
            MaxIntentos = 5
        });

        Assert.True(creation.Creado);
        Assert.Equal(73, creation.Mensaje.EmpresaId);
        Assert.Equal(sharedKey, creation.Mensaje.ClaveIdempotencia);
        Assert.Single(repository.Added);
        Assert.Equal(73, repository.Added[0].EmpresaId);
        Assert.Equal(2, repository.SaveChangesCalls);
    }

    private static MensajeOutbox CrearMensaje(
        long id,
        int empresaId,
        string claveIdempotencia,
        MensajeOutboxEstado estado,
        short maxIntentos)
    {
        var mensaje = new MensajeOutbox(
            empresaId,
            claveIdempotencia,
            "n7.3.g-qa",
            "{}",
            new DateTimeOffset(2027, 3, 7, 17, 0, 0, TimeSpan.Zero),
            maxIntentos);
        mensaje.SetId(id);

        if (estado == MensajeOutboxEstado.DeadLetter)
        {
            for (var i = 0; i < maxIntentos; i++)
            {
                mensaje.MarcarFallido(
                    $"n73g-failure-{i + 1}",
                    new DateTimeOffset(2027, 3, 7, 17, 10 + i, 0, TimeSpan.Zero));
            }
        }

        return mensaje;
    }

    private sealed class FixedCurrentUserService : ICurrentUserService
    {
        public FixedCurrentUserService(int? empresaId) => EmpresaId = empresaId;

        public string UserId => "n7.3.g-qa";
        public string? Nombre => "VAEP";
        public string? Email => "vaep@test.local";
        public string? Role => "Admin";
        public int? EmpresaId { get; }
        public int? SucursalId => null;
        public bool IsAuthenticated => EmpresaId.HasValue;
        public bool IsSuperAdmin => false;
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FakeRepository : IMensajeOutboxRepository
    {
        private readonly List<MensajeOutbox> _mensajes;
        private long _nextId = 9000;

        public FakeRepository(IEnumerable<MensajeOutbox> mensajes)
        {
            _mensajes = mensajes.ToList();
        }

        public Dictionary<(int EmpresaId, string Clave), MensajeOutbox> Existing { get; } = new();
        public List<MensajeOutbox> Added { get; } = new();
        public List<int> ClaimTenantIds { get; } = new();
        public int SaveChangesCalls { get; private set; }

        public Task<MensajeOutbox?> BuscarPorClaveIdempotenciaAsync(
            int empresaId,
            string claveIdempotencia,
            CancellationToken cancellationToken = default)
        {
            Existing.TryGetValue((empresaId, claveIdempotencia), out var mensaje);
            return Task.FromResult(mensaje);
        }

        public void Add(MensajeOutbox mensaje)
        {
            if (mensaje.Id == 0)
            {
                mensaje.SetId(_nextId++);
            }

            Added.Add(mensaje);
            _mensajes.Add(mensaje);
        }

        public Task<IReadOnlyList<MensajeOutbox>> ClaimPendientesAsync(
            int empresaId,
            int batchSize,
            DateTimeOffset nowUtc,
            string lockId,
            TimeSpan lockDuration,
            CancellationToken cancellationToken = default)
        {
            ClaimTenantIds.Add(empresaId);
            var claimed = _mensajes
                .Where(x => x.EmpresaId == empresaId && x.PuedeProcesarse(nowUtc))
                .Take(batchSize)
                .ToList();

            foreach (var item in claimed)
            {
                item.MarcarProcesando(lockId, nowUtc, lockDuration);
            }

            return Task.FromResult<IReadOnlyList<MensajeOutbox>>(claimed);
        }

        public Task<bool> MarcarEntregadoAsync(
            long id,
            string lockId,
            DateTimeOffset deliveredAtUtc,
            CancellationToken cancellationToken = default)
        {
            _mensajes.Single(x => x.Id == id).MarcarEntregado(lockId, deliveredAtUtc);
            return Task.FromResult(true);
        }

        public Task<bool> MarcarFallidoAsync(
            long id,
            string lockId,
            DateTimeOffset nowUtc,
            string error,
            CancellationToken cancellationToken = default)
        {
            var transitioned = _mensajes.Single(x => x.Id == id).MarcarFallido(lockId, error, nowUtc);
            return Task.FromResult(transitioned);
        }

        public Task<int> RecuperarLocksExpiradosAsync(
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<int> LimpiarEntregadosAsync(
            int empresaId,
            DateTimeOffset deliveredBeforeUtc,
            int batchSize,
            CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
            return Task.FromResult(1);
        }
    }
}
