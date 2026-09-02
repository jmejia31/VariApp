using InventoryApp.Application.DTOs.Bancos;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities.Bancos;
using Moq;
using Xunit;

namespace InventoryApp.Tests.Application.Bancos;

public class CuentaBancariaServiceUpdateTests
{
    private readonly Mock<ICuentaBancariaRepository> _mockRepo;
    private readonly CuentaBancariaService _service;

    public CuentaBancariaServiceUpdateTests()
    {
        _mockRepo = new Mock<ICuentaBancariaRepository>();
        _service = new CuentaBancariaService(_mockRepo.Object);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesNombre_WhenExists()
    {
        var cuenta = new CuentaBancaria(1, "Old Name", "123", "HNL", 10m);
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cuenta);

        await _service.UpdateAsync(1, new UpdateCuentaBancariaDto { Nombre = "New Name" });

        Assert.Equal("New Name", cuenta.Nombre);
        _mockRepo.Verify(r => r.Update(cuenta), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenNotExists()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((CuentaBancaria?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateAsync(1, new UpdateCuentaBancariaDto { Nombre = "New Name" }));
    }
}
