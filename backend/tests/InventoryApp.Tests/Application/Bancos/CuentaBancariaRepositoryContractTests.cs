using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities.Bancos;
using Moq;
using Xunit;

namespace InventoryApp.Tests.Application.Bancos;

public class CuentaBancariaRepositoryContractTests
{
    [Fact]
    public async Task GetByIdAsync_Returns_Expected_Cuenta()
    {
        var mockRepo = new Mock<ICuentaBancariaRepository>();
        var expectedCuenta = new CuentaBancaria(1, "Principal", "123456789", "HNL", 1000m);
        mockRepo.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(expectedCuenta);

        var result = await mockRepo.Object.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Principal", result.Nombre);
        Assert.Equal("123456789", result.NumeroCuenta);
    }

    [Fact]
    public async Task GetActivasAsync_Returns_Expected_List()
    {
        var mockRepo = new Mock<ICuentaBancariaRepository>();
        var expectedList = new List<CuentaBancaria>
        {
            new(1, "Principal", "123456789", "HNL", 1000m)
        };
        mockRepo.Setup(repo => repo.GetActivasAsync()).ReturnsAsync(expectedList);

        var result = await mockRepo.Object.GetActivasAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAllAsync_Returns_Expected_List()
    {
        var mockRepo = new Mock<ICuentaBancariaRepository>();
        var expectedList = new List<CuentaBancaria>
        {
            new(1, "Principal", "123456789", "HNL", 1000m)
        };
        mockRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync(expectedList);

        var result = await mockRepo.Object.GetAllAsync();

        Assert.Single(result);
    }
}
