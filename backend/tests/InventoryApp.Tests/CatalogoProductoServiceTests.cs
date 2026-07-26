using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class CatalogoProductoServiceTests
{
    private readonly Mock<ICatalogoProductoRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly CatalogoProductoService _service;

    public CatalogoProductoServiceTests()
    {
        _currentUser.Setup(x => x.UsuarioId).Returns(7);
        _currentUser.Setup(x => x.NombreUsuario).Returns("admin_pruebas");
        _service = new CatalogoProductoService(
            _repository.Object,
            _currentUser.Object,
            _auditoria.Object);
    }

    [Fact]
    public async Task GetAllAsync_Modelos_SinMarca_ListaTodos()
    {
        _repository
            .Setup(r => r.GetAllAsync(TipoCatalogoProducto.Modelo, null, null))
            .ReturnsAsync(new List<CatalogoProducto>
            {
                new() { Id = 1, Tipo = TipoCatalogoProducto.Modelo, Nombre = "S24", Activo = true }
            });

        var resultado = await _service.GetAllAsync(TipoCatalogoProducto.Modelo);

        Assert.Single(resultado);
        Assert.Equal("S24", resultado[0].Nombre);
    }

    [Fact]
    public async Task CreateAsync_ModeloSinMarca_LanzaReglaNegocio()
    {
        var dto = new CreateCatalogoProductoDto { Nombre = "S24" };

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAsync(TipoCatalogoProducto.Modelo, dto));

        Assert.Contains("marca", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_Color_NormalizaHexadecimalYAuditoria()
    {
        CatalogoProducto? guardado = null;
        _repository
            .Setup(r => r.ExisteNombreAsync(TipoCatalogoProducto.Color, "Azul", null, null))
            .ReturnsAsync(false);
        _repository
            .Setup(r => r.AddAsync(It.IsAny<CatalogoProducto>()))
            .Callback<CatalogoProducto>(c => guardado = c)
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.CreateAsync(TipoCatalogoProducto.Color, new CreateCatalogoProductoDto
        {
            Nombre = " Azul ",
            CodigoVisual = "#1d4ed8"
        });

        Assert.NotNull(guardado);
        Assert.Equal("Azul", guardado!.Nombre);
        Assert.Equal("#1D4ED8", guardado.CodigoVisual);
        Assert.Equal(7, guardado.CreadoPorUsuarioId);
        Assert.Equal("admin_pruebas", resultado.CreadoPorNombreUsuario);
    }

    [Fact]
    public async Task ValidarSeleccionProducto_ModeloDeOtraMarca_LanzaReglaNegocio()
    {
        _repository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CatalogoProducto
        {
            Id = 10,
            Tipo = TipoCatalogoProducto.Marca,
            Nombre = "Samsung",
            Activo = true
        });
        _repository.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(new CatalogoProducto
        {
            Id = 20,
            Tipo = TipoCatalogoProducto.Modelo,
            Nombre = "iPhone 15",
            CatalogoPadreId = 11,
            Activo = true
        });

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.ValidarSeleccionProductoAsync(null, null, 10, 20));

        Assert.Contains("no pertenece", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAsync_MarcaConModelos_BloqueaEliminacion()
    {
        var marca = new CatalogoProducto
        {
            Id = 1,
            Tipo = TipoCatalogoProducto.Marca,
            Nombre = "Samsung",
            Activo = true
        };
        marca.ElementosHijos.Add(new CatalogoProducto
        {
            Id = 2,
            Tipo = TipoCatalogoProducto.Modelo,
            Nombre = "A55",
            CatalogoPadreId = 1
        });
        _repository.Setup(r => r.GetByIdConRelacionesAsync(1)).ReturnsAsync(marca);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.DeleteAsync(TipoCatalogoProducto.Marca, 1));

        _repository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
