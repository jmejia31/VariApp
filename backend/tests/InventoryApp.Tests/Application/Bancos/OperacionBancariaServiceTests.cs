using InventoryApp.Application.Bancos;
using InventoryApp.Application.DTOs.Bancos;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Entities.Bancos;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests.Application.Bancos;

public class OperacionBancariaServiceTests
{
    private readonly Mock<ICuentaBancariaRepository> _mockCuentaRepo = new();
    private readonly Mock<IMovimientoFinancieroRepository> _mockMovimientoRepo = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly OperacionBancariaService _service;

    public OperacionBancariaServiceTests()
    {
        _mockUnitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(func => func());
        _service = new OperacionBancariaService(_mockCuentaRepo.Object, _mockMovimientoRepo.Object, _mockUnitOfWork.Object);
    }

    private static CuentaBancaria CreateActiveCuenta(string numero = "123", string moneda = "HNL") =>
        new(1, "Test Bank", numero, moneda, 1000m);

    [Fact]
    public async Task RegistrarDeposito_CreatesIngreso_WhenValid()
    {
        var dto = new DepositoBancarioDto { CuentaId = 1, Monto = 500m, Referencia = "REF-1", IdempotencyKey = "key1" };
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateActiveCuenta());
        await _service.RegistrarDepositoAsync(dto, 99);
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.Is<MovimientoFinanciero>(m => m.Tipo == TipoMovimientoFinanciero.Ingreso && m.Monto == 500m && m.CreadoPorUsuarioId == 99 && m.Descripcion!.Contains("key1"))), Times.Once);
        _mockMovimientoRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegistrarRetiro_CreatesEgreso_WhenValid()
    {
        var dto = new RetiroBancarioDto { CuentaId = 1, Monto = 200m, Referencia = "REF-2", IdempotencyKey = "key2" };
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateActiveCuenta());
        await _service.RegistrarRetiroAsync(dto, 99);
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.Is<MovimientoFinanciero>(m => m.Tipo == TipoMovimientoFinanciero.Egreso && m.Monto == 200m && m.CreadoPorUsuarioId == 99 && m.Descripcion!.Contains("key2"))), Times.Once);
    }

    [Fact]
    public async Task RegistrarTransferencia_CreatesIngresoAndEgreso_WhenValid()
    {
        var dto = new TransferenciaBancariaDto { CuentaId = 1, CuentaDestinoId = 2, Monto = 300m, Referencia = "REF-3", IdempotencyKey = "key3" };
        var origen = CreateActiveCuenta("123");
        var destino = CreateActiveCuenta("456");
        typeof(CuentaBancaria).GetProperty("Id")?.SetValue(origen, 1);
        typeof(CuentaBancaria).GetProperty("Id")?.SetValue(destino, 2);
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(origen);
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(destino);
        await _service.RegistrarTransferenciaAsync(dto, 99);
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.Is<MovimientoFinanciero>(m => m.Tipo == TipoMovimientoFinanciero.Egreso && m.Monto == 300m && m.Descripcion!.Contains("key3-Egreso"))), Times.Once);
        _mockMovimientoRepo.Verify(r => r.AddAsync(It.Is<MovimientoFinanciero>(m => m.Tipo == TipoMovimientoFinanciero.Ingreso && m.Monto == 300m && m.Descripcion!.Contains("key3-Ingreso"))), Times.Once);
    }

    [Fact]
    public async Task RegistrarDeposito_ThrowsArgumentException_WhenCuentaNotFound()
    {
        var dto = new DepositoBancarioDto { CuentaId = 999, Monto = 500m, Referencia = "REF-1", IdempotencyKey = "key1" };
        _mockCuentaRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((CuentaBancaria?)null);
        await Assert.ThrowsAsync<ArgumentException>(() => _service.RegistrarDepositoAsync(dto, 99));
    }
}
