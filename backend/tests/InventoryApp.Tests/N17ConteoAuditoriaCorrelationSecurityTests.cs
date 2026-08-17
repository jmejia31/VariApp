using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N17ConteoAuditoriaCorrelationSecurityTests
{
    [Fact]
    public async Task AuditoriaConteo_UsaTraceIdentifierSaneado_YNoCorrelationBrutaDelCliente()
    {
        var repository = new AuditoriaRepositoryFake();
        var currentUser = new CurrentUserFake();
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "corr-conteo-normalizada-77"
        };
        context.Request.Headers["X-Correlation-ID"] = "<script>corr-conteo-controlada-por-cliente</script>";
        context.Request.Headers["User-Agent"] = "N17-conteo-security-test";
        var accessor = new HttpContextAccessor { HttpContext = context };
        var service = new AuditoriaService(
            repository,
            currentUser,
            accessor,
            NullLogger<AuditoriaService>.Instance);

        await service.RegistrarAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Cerrar,
            "Conteo físico cerrado",
            referenciaId: 77,
            entidad: nameof(ConteoInventario));

        var registro = Assert.IsType<RegistroAuditoria>(repository.UltimoRegistro);
        Assert.Equal("corr-conteo-normalizada-77", registro.CorrelationId);
        Assert.DoesNotContain("script", registro.CorrelationId!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(77, registro.ReferenciaId);
        Assert.Equal(nameof(ConteoInventario), registro.Entidad);
        Assert.Equal(ModuloSistema.MovimientosInventario, registro.Modulo);
        Assert.Equal(AccionPermiso.Cerrar, registro.Accion);
        Assert.True(repository.Guardado);
    }

    private sealed class AuditoriaRepositoryFake : IAuditoriaRepository
    {
        public RegistroAuditoria? UltimoRegistro { get; private set; }
        public bool Guardado { get; private set; }

        public Task AddAsync(RegistroAuditoria registro)
        {
            UltimoRegistro = registro;
            return Task.CompletedTask;
        }

        public Task<(List<RegistroAuditoria> Items, int TotalCount)> GetFilteredAsync(AuditoriaFiltroDto filtro) =>
            Task.FromResult((new List<RegistroAuditoria>(), 0));

        public Task<bool> SaveChangesAsync()
        {
            Guardado = true;
            return Task.FromResult(true);
        }
    }

    private sealed class CurrentUserFake : ICurrentUserService
    {
        public int? UsuarioId => 9002;
        public string? NombreUsuario => "n17-security";
        public string? NombreCompleto => "N1.7 Security Test";
        public int? RolId => 10;
        public bool EsAdministrador => false;
        public bool EstaAutenticado => true;
    }
}
