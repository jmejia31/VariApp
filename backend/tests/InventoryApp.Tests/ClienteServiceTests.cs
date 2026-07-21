using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class ClienteServiceTests
{
    private readonly Mock<IClienteRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IAuditoriaService> _auditoriaMock = new();
    private readonly ClienteService _service;

    public ClienteServiceTests()
    {
        _currentUserMock.Setup(c => c.UsuarioId).Returns(1);
        _currentUserMock.Setup(c => c.NombreUsuario).Returns("admin");
        _service = new ClienteService(_repoMock.Object, _currentUserMock.Object, _auditoriaMock.Object);
    }

    [Fact]
    public async Task CreateAsync_Nombre_Duplicado_Lanza_Excepcion()
    {
        _repoMock.Setup(r => r.ExisteNombreAsync("Juan Pérez", null)).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAsync(new CreateClienteDto { Nombre = "Juan Pérez" }));
    }

    [Fact]
    public async Task DeleteAsync_Con_Ventas_Asociadas_Aplica_Eliminacion_Logica()
    {
        var cliente = new Cliente { Id = 1, Nombre = "Juan Pérez", Activo = true };
        cliente.Ventas.Add(new Venta { NumeroVenta = "VEN-000001", Estado = EstadoDocumento.Confirmada, Total = 100 });

        _repoMock.Setup(r => r.GetByIdConVentasAsync(1)).ReturnsAsync(cliente);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.DeleteAsync(1);

        Assert.True(resultado);
        Assert.False(cliente.Activo);
        _repoMock.Verify(r => r.Remove(It.IsAny<Cliente>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Guarda_Usuario_Creador()
    {
        Cliente? creado = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Cliente>()))
            .Callback<Cliente>(c => creado = c)
            .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        await _service.CreateAsync(new CreateClienteDto { Nombre = "María López" });

        Assert.NotNull(creado);
        Assert.Equal(1, creado!.CreadoPorUsuarioId);
        Assert.True(creado.Activo);
    }
}
