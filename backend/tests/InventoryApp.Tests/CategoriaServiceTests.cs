using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class CategoriaServiceTests
{
    private readonly Mock<ICategoriaRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly CategoriaService _service;

    public CategoriaServiceTests()
    {
        _currentUserMock.Setup(c => c.UsuarioId).Returns(1);
        _currentUserMock.Setup(c => c.NombreUsuario).Returns("admin");
        _service = new CategoriaService(_repoMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task CreateAsync_Guarda_Usuario_Creador()
    {
        _repoMock.Setup(r => r.ExisteNombreAsync("Electrónica", null)).ReturnsAsync(false);

        Categoria? categoriaCreada = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Categoria>()))
            .Callback<Categoria>(c => categoriaCreada = c)
            .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.CreateAsync(new CreateCategoriaDto { Nombre = "Electrónica" });

        Assert.NotNull(categoriaCreada);
        Assert.Equal(1, categoriaCreada!.CreadoPorUsuarioId);
        Assert.Equal("admin", categoriaCreada.CreadoPorNombreUsuario);
        Assert.True(categoriaCreada.Activa);
    }

    [Fact]
    public async Task CreateAsync_Nombre_Duplicado_Lanza_Excepcion()
    {
        _repoMock.Setup(r => r.ExisteNombreAsync("Electrónica", null)).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAsync(new CreateCategoriaDto { Nombre = "Electrónica" }));
    }

    [Fact]
    public async Task DeleteAsync_Con_Productos_Asociados_No_Elimina_Fisicamente()
    {
        var categoria = new Categoria { Id = 1, Nombre = "Accesorios", Activa = true };
        categoria.Productos.Add(new Producto { Nombre = "Mouse", Marca = "X", Modelo = "Y" });

        _repoMock.Setup(r => r.GetByIdConProductosAsync(1)).ReturnsAsync(categoria);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.DeleteAsync(1));

        Assert.False(categoria.Activa);
        _repoMock.Verify(r => r.Remove(It.IsAny<Categoria>()), Times.Never);
    }
}
