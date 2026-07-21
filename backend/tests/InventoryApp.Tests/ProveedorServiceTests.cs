using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class ProveedorServiceTests
{
    private readonly Mock<IProveedorRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IAuditoriaService> _auditoriaMock = new();
    private readonly ProveedorService _service;

    public ProveedorServiceTests()
    {
        _currentUserMock.Setup(c => c.UsuarioId).Returns(1);
        _currentUserMock.Setup(c => c.NombreUsuario).Returns("admin");
        _service = new ProveedorService(_repoMock.Object, _currentUserMock.Object, _auditoriaMock.Object);
    }

    [Fact]
    public async Task CreateAsync_Nombre_Duplicado_Lanza_Excepcion()
    {
        _repoMock.Setup(r => r.ExisteNombreAsync("Tech Import SA", null)).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAsync(new CreateProveedorDto { Nombre = "Tech Import SA" }));
    }

    [Fact]
    public async Task DeleteAsync_Con_Compras_Asociadas_Aplica_Eliminacion_Logica()
    {
        var proveedor = new Proveedor { Id = 1, Nombre = "Tech Import SA", Activo = true };
        proveedor.Compras.Add(new Compra { NumeroCompra = "COM-000001", Estado = EstadoDocumento.Confirmada, Total = 100 });

        _repoMock.Setup(r => r.GetByIdConComprasAsync(1)).ReturnsAsync(proveedor);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.DeleteAsync(1);

        Assert.True(resultado);
        Assert.False(proveedor.Activo);
        Assert.True(proveedor.Eliminado);
        Assert.Equal(1, proveedor.EliminadoPorUsuarioId);
        _repoMock.Verify(r => r.Remove(It.IsAny<Proveedor>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Sin_Compras_Aplica_Eliminacion_Logica()
    {
        var proveedor = new Proveedor { Id = 1, Nombre = "Nuevo Proveedor", Activo = true };
        _repoMock.Setup(r => r.GetByIdConComprasAsync(1)).ReturnsAsync(proveedor);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.DeleteAsync(1);

        Assert.True(resultado);
        Assert.False(proveedor.Activo);
        Assert.True(proveedor.Eliminado);
        Assert.Equal(1, proveedor.EliminadoPorUsuarioId);
        _repoMock.Verify(r => r.Remove(It.IsAny<Proveedor>()), Times.Never);
    }
}
