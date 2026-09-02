using InventoryApp.Application.DTOs.Bancos;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities.Bancos;
using InventoryApp.Domain.Enums.Bancos;
using Moq;
using Xunit;

namespace InventoryApp.Tests.Application.Bancos;

public class CuentaBancariaServiceTests
{
    private readonly Mock<ICuentaBancariaRepository> _mockRepo;
    private readonly CuentaBancariaService _service;

    public CuentaBancariaServiceTests()
    {
        _mockRepo = new Mock<ICuentaBancariaRepository>();
        _service = new CuentaBancariaService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenExists()
    {
        var cuenta = new CuentaBancaria(1, "Test", "123", "HNL", 10m);
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cuenta);

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Test", result.Nombre);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((CuentaBancaria?)null);

        var result = await _service.GetByIdAsync(1);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_CreatesAndReturnsDto()
    {
        var dto = new CreateCuentaBancariaDto { BancoId = 1, Nombre = "N", NumeroCuenta = "123", Moneda = "HNL", SaldoInicial = 10m };
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<CuentaBancaria>())).Returns(Task.CompletedTask);
        _mockRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _service.AddAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("N", result.Nombre);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<CuentaBancaria>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ActivarAsync_ActivatesAndSaves()
    {
        var cuenta = new CuentaBancaria(1, "Test", "123", "HNL", 10m);
        cuenta.Desactivar();
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cuenta);

        await _service.ActivarAsync(1);

        Assert.Equal(EstadoCuentaBancaria.Activa, cuenta.Estado);
        _mockRepo.Verify(r => r.Update(cuenta), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
