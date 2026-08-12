using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;
using MetodoPagoEntity = InventoryApp.Domain.Entities.Catalogos.MetodoPago;

namespace InventoryApp.Tests;

public class MetodoPagoServiceTests
{
    private readonly Mock<IMetodoPagoRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly MetodoPagoService _service;

    public MetodoPagoServiceTests()
    {
        _currentUser.Setup(x => x.UsuarioId).Returns(7);
        _currentUser.Setup(x => x.NombreUsuario).Returns("admin.pruebas");
        _auditoria
            .Setup(x => x.RegistrarAsync(
                It.IsAny<ModuloSistema>(),
                It.IsAny<AccionPermiso>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        _service = new MetodoPagoService(
            _repository.Object,
            _currentUser.Object,
            _auditoria.Object,
            new FakeUnitOfWork());
    }

    [Fact]
    public async Task CreateAsync_NormalizaCodigo_CanonizaMetadata_YAudita()
    {
        MetodoPagoEntity? creado = null;
        _repository.Setup(x => x.ExisteCodigoAsync("TRANSFERENCIA_BAC", null)).ReturnsAsync(false);
        _repository
            .Setup(x => x.AddAsync(It.IsAny<MetodoPagoEntity>()))
            .Callback<MetodoPagoEntity>(x => creado = x)
            .Returns(Task.CompletedTask);
        _repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var resultado = await _service.CreateAsync(new CreateMetodoPagoDto
        {
            Codigo = " transferencia_bac ",
            Nombre = " Transferencia BAC ",
            Tipo = " Transferencia ",
            Activo = true,
            RequiereReferencia = true,
            RequiereBanco = true,
            Orden = 3,
            Metadata = "{\"z\":2,\"a\":1}"
        });

        Assert.NotNull(creado);
        Assert.Equal("TRANSFERENCIA_BAC", creado!.Codigo);
        Assert.Equal("Transferencia BAC", creado.Nombre);
        Assert.Equal("Transferencia", creado.Tipo);
        Assert.Equal("{\"a\":1,\"z\":2}", creado.Metadata);
        Assert.Equal(7, creado.CreadoPorUsuarioId);
        Assert.Equal("admin.pruebas", creado.CreadoPorNombreUsuario);
        Assert.Equal("TRANSFERENCIA_BAC", resultado.Codigo);

        _auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.MetodosPago,
            AccionPermiso.Crear,
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string?>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CodigoDuplicado_FallaCerradoSinPersistir()
    {
        _repository.Setup(x => x.ExisteCodigoAsync("EFECTIVO", null)).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateAsync(new CreateMetodoPagoDto
        {
            Codigo = " efectivo ",
            Nombre = "Efectivo",
            Tipo = "Efectivo"
        }));

        _repository.Verify(x => x.AddAsync(It.IsAny<MetodoPagoEntity>()), Times.Never);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ActualizaCampos_MarcaUsuario_YAuditaAntesDespues()
    {
        var metodo = new MetodoPagoEntity
        {
            Id = 10,
            Codigo = "TARJETA",
            Nombre = "Tarjeta",
            Tipo = "Tarjeta",
            Activo = true,
            Orden = 1
        };
        _repository.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(metodo);
        _repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var resultado = await _service.UpdateAsync(10, new UpdateMetodoPagoDto
        {
            Nombre = " Tarjeta POS ",
            Tipo = " Tarjeta ",
            Activo = true,
            RequiereReferencia = true,
            RequiereBanco = true,
            Orden = 2,
            Metadata = "{\"terminal\":true}"
        });

        Assert.NotNull(resultado);
        Assert.Equal("Tarjeta POS", metodo.Nombre);
        Assert.Equal(2, metodo.Orden);
        Assert.True(metodo.RequiereReferencia);
        Assert.True(metodo.RequiereBanco);
        Assert.Equal(7, metodo.ActualizadoPorUsuarioId);
        Assert.Equal("admin.pruebas", metodo.ActualizadoPorNombreUsuario);
        Assert.NotEqual(default, metodo.FechaActualizacion);
        _repository.Verify(x => x.Update(metodo), Times.Once);

        _auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.MetodosPago,
            AccionPermiso.Editar,
            It.IsAny<string>(),
            10,
            It.IsAny<string?>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CambiarEstadoAsync_DesactivaSinEliminar_YAudita()
    {
        var metodo = new MetodoPagoEntity
        {
            Id = 11,
            Codigo = "TRANSFERENCIA",
            Nombre = "Transferencia",
            Tipo = "Transferencia",
            Activo = true
        };
        _repository.Setup(x => x.GetByIdAsync(11)).ReturnsAsync(metodo);
        _repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var resultado = await _service.CambiarEstadoAsync(11, false);

        Assert.NotNull(resultado);
        Assert.False(metodo.Activo);
        Assert.False(metodo.Eliminado);
        _auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.MetodosPago,
            AccionPermiso.Desactivar,
            It.IsAny<string>(),
            11,
            It.IsAny<string?>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_AplicaEliminacionLogicaConTrazabilidad()
    {
        var metodo = new MetodoPagoEntity
        {
            Id = 12,
            Codigo = "OTRO",
            Nombre = "Otro",
            Tipo = "Otro",
            Activo = true
        };
        _repository.Setup(x => x.GetByIdAsync(12)).ReturnsAsync(metodo);
        _repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var eliminado = await _service.DeleteAsync(12);

        Assert.True(eliminado);
        Assert.True(metodo.Eliminado);
        Assert.False(metodo.Activo);
        Assert.NotNull(metodo.FechaEliminacion);
        Assert.Equal(7, metodo.EliminadoPorUsuarioId);
        Assert.Equal(7, metodo.ActualizadoPorUsuarioId);
        _repository.Verify(x => x.Update(metodo), Times.Once);

        _auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.MetodosPago,
            AccionPermiso.EliminarLogico,
            It.IsAny<string>(),
            12,
            It.IsAny<string?>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ReordenarAsync_RechazaIdsDuplicadosSinPersistir()
    {
        var cambios = new[]
        {
            new ReordenarMetodoPagoDto { Id = 3, Orden = 1 },
            new ReordenarMetodoPagoDto { Id = 3, Orden = 2 }
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ReordenarAsync(cambios));

        _repository.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }
}