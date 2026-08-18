using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N21SolicitudCompraServiceRegressionTests
{
    [Fact]
    public async Task Update_invalido_no_muta_el_borrador_existente()
    {
        var solicitud = CrearBorrador();
        var detalleOriginal = solicitud.Detalles.Single();
        var repo = new RepoStub(solicitud);
        var unitOfWork = new UnitOfWorkStub();
        var service = new SolicitudCompraService(repo, new CurrentUserStub(), unitOfWork);

        var dto = new UpdateSolicitudCompraDto
        {
            ProveedorId = 99,
            Notas = "debe fallar",
            Detalles =
            {
                new SolicitudCompraDetalleInputDto
                {
                    ProductoId = 1,
                    CantidadSolicitada = 0
                }
            }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(17, dto));

        Assert.Equal(7, solicitud.ProveedorId);
        Assert.Equal("original", solicitud.Notas);
        Assert.Same(detalleOriginal, solicitud.Detalles.Single());
        Assert.Equal(0, repo.SaveCalls);
        Assert.Equal(0, repo.LockCalls);
        Assert.Equal(0, unitOfWork.Calls);
    }

    [Fact]
    public async Task Enviar_exige_usuario_autenticado_y_no_muta_estado()
    {
        var solicitud = CrearBorrador();
        var repo = new RepoStub(solicitud);
        var unitOfWork = new UnitOfWorkStub();
        var service = new SolicitudCompraService(repo, new CurrentUserStub(autenticado: false), unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.EnviarAsync(17));

        Assert.Equal(EstadoSolicitudCompra.Borrador, solicitud.Estado);
        Assert.Null(solicitud.FechaSolicitudUtc);
        Assert.Equal(0, repo.SaveCalls);
        Assert.Equal(0, repo.LockCalls);
        Assert.Equal(0, unitOfWork.Calls);
    }

    [Fact]
    public async Task Enviar_serializa_transicion_con_uow_y_lectura_exclusiva()
    {
        var solicitud = CrearBorrador();
        var repo = new RepoStub(solicitud);
        var unitOfWork = new UnitOfWorkStub();
        var service = new SolicitudCompraService(repo, new CurrentUserStub(), unitOfWork);

        await service.EnviarAsync(17);

        Assert.Equal(EstadoSolicitudCompra.Solicitada, solicitud.Estado);
        Assert.Equal(1, repo.SaveCalls);
        Assert.Equal(1, repo.LockCalls);
        Assert.Equal(1, unitOfWork.Calls);
    }

    [Fact]
    public async Task Aprobar_dos_veces_falla_cerrado_sin_segunda_persistencia()
    {
        var solicitud = CrearBorrador();
        solicitud.Solicitar(41, "Solicitante", DateTime.UtcNow);
        var repo = new RepoStub(solicitud);
        var unitOfWork = new UnitOfWorkStub();
        var service = new SolicitudCompraService(repo, new CurrentUserStub(), unitOfWork);

        await service.AprobarAsync(17);
        Assert.Equal(EstadoSolicitudCompra.Aprobada, solicitud.Estado);
        Assert.Equal(1, repo.SaveCalls);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AprobarAsync(17));
        Assert.Equal(1, repo.SaveCalls);
        Assert.Equal(2, repo.LockCalls);
        Assert.Equal(2, unitOfWork.Calls);
    }

    [Fact]
    public async Task Paginacion_y_rango_temporal_se_validan_antes_del_repositorio()
    {
        var repo = new RepoStub(null);
        var service = new SolicitudCompraService(repo, new CurrentUserStub(), new UnitOfWorkStub());
        var filtro = new SolicitudCompraFiltroDto
        {
            Page = -4,
            PageSize = 999,
            Desde = new DateTime(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc),
            Hasta = new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetPagedAsync(filtro));
        Assert.Equal(0, repo.PagedCalls);
    }

    private static SolicitudCompra CrearBorrador()
    {
        var detalle = new SolicitudCompraDetalle
        {
            ProductoId = 1,
            Observacion = "línea original"
        };
        detalle.EstablecerCantidad(2);
        detalle.EstablecerCostoEstimado(10);

        return new SolicitudCompra
        {
            Id = 17,
            NumeroSolicitud = "SC-000017",
            ProveedorId = 7,
            Notas = "original",
            Detalles = new List<SolicitudCompraDetalle> { detalle }
        };
    }

    private sealed class CurrentUserStub : ICurrentUserService
    {
        public CurrentUserStub(bool autenticado = true) => EstaAutenticado = autenticado;
        public int? UsuarioId => EstaAutenticado ? 42 : null;
        public string? NombreUsuario => EstaAutenticado ? "qa" : null;
        public string? NombreCompleto => EstaAutenticado ? "QA Usuario" : null;
        public int? RolId => EstaAutenticado ? 2 : null;
        public bool EsAdministrador => false;
        public bool EstaAutenticado { get; }
    }

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public int Calls { get; private set; }

        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            Calls++;
            await operation();
        }
    }

    private sealed class RepoStub : ISolicitudCompraRepository
    {
        private readonly SolicitudCompra? _solicitud;
        public RepoStub(SolicitudCompra? solicitud) => _solicitud = solicitud;
        public int SaveCalls { get; private set; }
        public int PagedCalls { get; private set; }
        public int LockCalls { get; private set; }

        public Task<(IReadOnlyList<SolicitudCompra> Items, int Total)> GetPagedAsync(SolicitudCompraFiltroDto filtro)
        {
            PagedCalls++;
            IReadOnlyList<SolicitudCompra> items = _solicitud is null ? [] : [_solicitud];
            return Task.FromResult((items, items.Count));
        }

        public Task<SolicitudCompra?> GetByIdAsync(int id, bool tracking = false) =>
            Task.FromResult(_solicitud is not null && _solicitud.Id == id ? _solicitud : null);

        public Task<SolicitudCompra?> GetByIdForUpdateAsync(int id)
        {
            LockCalls++;
            return Task.FromResult(_solicitud is not null && _solicitud.Id == id ? _solicitud : null);
        }

        public Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null) => Task.FromResult(false);
        public Task AddAsync(SolicitudCompra solicitud) => Task.CompletedTask;
        public Task SaveChangesAsync()
        {
            SaveCalls++;
            return Task.CompletedTask;
        }
    }
}
