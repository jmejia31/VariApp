using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N19TrazabilidadAuditSecurityTests
{
    [Fact]
    public async Task Crear_lote_registra_auditoria_estricta_sin_exponer_codigo_trazable()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        variante.ConfigurarTrazabilidad(true, false, true, 30);

        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetLoteByCodigoAsync(11, "LOTE-AUD", false))
            .ReturnsAsync((LoteInventario?)null);
        repo.Setup(x => x.TryAddLoteAsync(It.IsAny<LoteInventario>())).ReturnsAsync(true);

        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);

        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UsuarioId).Returns(99);
        current.SetupGet(x => x.NombreUsuario).Returns("n19-audit-test");

        var unit = new Mock<IUnitOfWork>();
        unit.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> operation) => operation());

        var auditoria = new Mock<IAuditoriaService>();
        auditoria.Setup(x => x.RegistrarEstrictoAsync(
                It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var service = new TrazabilidadInventarioService(
            repo.Object, variantes.Object, current.Object, unit.Object, auditoria.Object);

        await service.CrearLoteAsync(new CrearLoteInventarioRequest
        {
            ProductoVarianteId = 11,
            Codigo = " lote-aud ",
            FechaFabricacion = new DateTime(2026, 8, 1),
            FechaVencimiento = new DateTime(2027, 8, 1)
        });

        auditoria.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Crear,
            It.Is<string>(d => d.Contains("Lote", StringComparison.OrdinalIgnoreCase)),
            It.IsAny<int?>(),
            "LoteInventario",
            null,
            It.Is<object?>(payload => NoExponePropiedad(payload, "Codigo")),
            null,
            "Exito",
            null), Times.Once);
    }

    [Fact]
    public async Task Crear_lote_propaga_fallo_de_auditoria_estricta_dentro_de_transaccion()
    {
        var variante = new ProductoVariante { Id = 11, Activo = true };
        variante.ConfigurarTrazabilidad(true, false, false);

        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetLoteByCodigoAsync(11, "LOTE-FAIL", false))
            .ReturnsAsync((LoteInventario?)null);
        repo.Setup(x => x.TryAddLoteAsync(It.IsAny<LoteInventario>())).ReturnsAsync(true);

        var variantes = new Mock<IProductoVarianteRepository>();
        variantes.Setup(x => x.GetByIdForUpdateAsync(11)).ReturnsAsync(variante);
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UsuarioId).Returns(99);
        current.SetupGet(x => x.NombreUsuario).Returns("n19-audit-test");
        var unit = new Mock<IUnitOfWork>();
        unit.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> operation) => operation());
        var auditoria = new Mock<IAuditoriaService>();
        auditoria.Setup(x => x.RegistrarEstrictoAsync(
                It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("audit-store-down"));

        var service = new TrazabilidadInventarioService(
            repo.Object, variantes.Object, current.Object, unit.Object, auditoria.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CrearLoteAsync(new CrearLoteInventarioRequest
        {
            ProductoVarianteId = 11,
            Codigo = "LOTE-FAIL"
        }));

        Assert.Equal("audit-store-down", ex.Message);
        unit.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Once);
    }

    [Fact]
    public async Task Baja_idempotente_no_duplica_auditoria()
    {
        var serie = new SerieInventario { Id = 5, ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad("SN-SENSIBLE");
        serie.DarDeBaja();

        var repo = new Mock<ITrazabilidadInventarioRepository>();
        repo.Setup(x => x.GetSerieByIdAsync(5, true)).ReturnsAsync(serie);
        var variantes = new Mock<IProductoVarianteRepository>();
        var current = new Mock<ICurrentUserService>();
        var unit = new Mock<IUnitOfWork>();
        unit.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> operation) => operation());
        var auditoria = new Mock<IAuditoriaService>();

        var service = new TrazabilidadInventarioService(
            repo.Object, variantes.Object, current.Object, unit.Object, auditoria.Object);

        var resultado = await service.DarDeBajaSerieAsync(5);

        Assert.Equal(EstadoSerieInventario.Baja, resultado.Estado);
        auditoria.Verify(x => x.RegistrarEstrictoAsync(
            It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(),
            It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    private static bool NoExponePropiedad(object? payload, string nombre) =>
        payload is not null && payload.GetType().GetProperty(nombre) is null;
}
