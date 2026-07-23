using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class FinanzasServiceTests
{
    private readonly Mock<IMovimientoFinancieroRepository> _movRepoMock = new();
    private readonly Mock<IRevisionFinancieraRepository> _revisionRepoMock = new();
    private readonly Mock<IVentaRepository> _ventaRepoMock = new();
    private readonly Mock<ICompraRepository> _compraRepoMock = new();
    private readonly Mock<IProductoRepository> _productoRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IUsuarioScopeService> _usuarioScopeMock = new();
    private readonly FinanzasService _service;

    public FinanzasServiceTests()
    {
        _currentUserMock.Setup(c => c.UsuarioId).Returns(1);
        _currentUserMock.Setup(c => c.NombreUsuario).Returns("admin");
        _currentUserMock.Setup(c => c.NombreCompleto).Returns("Administrador");
        _usuarioScopeMock.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Administrador", true));
        _movRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
        _revisionRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        _service = new FinanzasService(
            _movRepoMock.Object,
            _revisionRepoMock.Object,
            _ventaRepoMock.Object,
            _compraRepoMock.Object,
            _productoRepoMock.Object,
            _currentUserMock.Object,
            _usuarioScopeMock.Object);
    }

    [Fact]
    public async Task RegistrarMovimientoManualAsync_Guarda_Usuario_Y_No_Es_Automatico()
    {
        MovimientoFinanciero? creado = null;
        _movRepoMock.Setup(r => r.AddAsync(It.IsAny<MovimientoFinanciero>()))
            .Callback<MovimientoFinanciero>(m => creado = m)
            .Returns(Task.CompletedTask);

        await _service.RegistrarMovimientoManualAsync(new CreateMovimientoManualDto
        {
            Tipo = "Egreso", Categoria = "GastoOperativo", Concepto = "Pago de internet", Monto = 500
        });

        Assert.NotNull(creado);
        Assert.False(creado!.EsAutomatico);
        Assert.Equal(1, creado.CreadoPorUsuarioId);
        Assert.Equal("Manual", creado.ModuloOrigen);
    }

    [Fact]
    public async Task AnularMovimientoAsync_Bloquea_Movimientos_Automaticos()
    {
        var movimiento = new MovimientoFinanciero { Id = 1, EsAutomatico = true, ModuloOrigen = "Venta" };
        _movRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(movimiento);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AnularMovimientoAsync(1, "motivo"));
    }

    [Fact]
    public async Task AnularMovimientoAsync_Permite_Anular_Manuales()
    {
        var movimiento = new MovimientoFinanciero { Id = 1, EsAutomatico = false, ModuloOrigen = "Manual" };
        _movRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(movimiento);

        var resultado = await _service.AnularMovimientoAsync(1, "Error de registro");

        Assert.Equal("Anulado", resultado!.Estado);
        Assert.Equal("Error de registro", movimiento.MotivoAnulacion);
    }

    [Fact]
    public async Task RegistrarRevisionAsync_Guarda_Usuario_Revisor_Administrador()
    {
        var resultado = await _service.RegistrarRevisionAsync(new CreateRevisionFinancieraDto
        {
            FechaDesde = DateTime.UtcNow.AddDays(-30),
            FechaHasta = DateTime.UtcNow,
            EstadoRevision = "Revisado",
            Observaciones = "Todo cuadra"
        });

        Assert.Equal("Administrador", resultado.RevisadoPorNombreUsuario);
        Assert.Equal("Revisado", resultado.EstadoRevision);
    }

    [Fact]
    public async Task RegistrarRevisionAsync_Usuario_No_Administrador_Es_Rechazado()
    {
        _usuarioScopeMock.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(2, 2, "Vendedor", false));

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.RegistrarRevisionAsync(new CreateRevisionFinancieraDto
        {
            FechaDesde = DateTime.UtcNow.AddDays(-30),
            FechaHasta = DateTime.UtcNow,
            EstadoRevision = "Revisado"
        }));

        _revisionRepoMock.Verify(r => r.AddAsync(It.IsAny<RevisionFinanciera>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarRevisionAsync_Fecha_Hasta_Menor_A_Desde_Lanza_Excepcion()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.RegistrarRevisionAsync(new CreateRevisionFinancieraDto
        {
            FechaDesde = DateTime.UtcNow,
            FechaHasta = DateTime.UtcNow.AddDays(-10)
        }));
    }

    [Fact]
    public async Task GetResumenAsync_Sesion_Sin_Usuario_Dinamico_Falla_Cerrada()
    {
        _usuarioScopeMock.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync((UsuarioScopeActual?)null);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => _service.GetResumenAsync());
        _movRepoMock.Verify(r => r.GetFilteredAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()), Times.Never);
    }
}
