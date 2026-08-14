using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class SucursalServiceTests
{
    private readonly Mock<ISucursalRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IAuditoriaService> _auditoriaMock = new();
    private readonly SucursalService _service;

    public SucursalServiceTests()
    {
        _currentUserMock.Setup(c => c.UsuarioId).Returns(7);
        _currentUserMock.Setup(c => c.NombreUsuario).Returns("admin-sucursales");
        _service = new SucursalService(_repoMock.Object, _currentUserMock.Object, _auditoriaMock.Object);
    }

    [Fact]
    public async Task CreateAsync_NormalizaCodigo_Y_GuardaAuditoriaCreacion()
    {
        _repoMock.Setup(r => r.ExisteCodigoAsync("TGU-01", null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        Sucursal? creada = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Sucursal>()))
            .Callback<Sucursal>(s => creada = s)
            .Returns(Task.CompletedTask);

        var resultado = await _service.CreateAsync(new CreateSucursalDto
        {
            Codigo = " tgu-01 ",
            Nombre = " Sucursal Centro ",
            ZonaHoraria = "America/Tegucigalpa"
        });

        Assert.NotNull(creada);
        Assert.Equal("TGU-01", creada!.Codigo);
        Assert.Equal("Sucursal Centro", creada.Nombre);
        Assert.True(creada.Activa);
        Assert.False(creada.Eliminado);
        Assert.Equal(7, creada.CreadoPorUsuarioId);
        Assert.Equal("admin-sucursales", creada.CreadoPorNombreUsuario);
        Assert.Equal("TGU-01", resultado.Codigo);
    }

    [Fact]
    public async Task CreateAsync_CodigoDuplicado_LanzaBusinessRule()
    {
        _repoMock.Setup(r => r.ExisteCodigoAsync("TGU-01", null)).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAsync(new CreateSucursalDto
            {
                Codigo = "TGU-01",
                Nombre = "Sucursal Centro",
                ZonaHoraria = "America/Tegucigalpa"
            }));

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Sucursal>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZonaHorariaInvalida_LanzaBusinessRule()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAsync(new CreateSucursalDto
            {
                Codigo = "TGU-01",
                Nombre = "Sucursal Centro",
                ZonaHoraria = "Zona/Que-No-Existe"
            }));
    }

    [Fact]
    public async Task CambiarEstadoAsync_MismoEstado_EsIdempotente()
    {
        var sucursal = new Sucursal
        {
            Id = 10,
            Codigo = "TGU-01",
            Nombre = "Centro",
            ZonaHoraria = "America/Tegucigalpa",
            Activa = true
        };
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(sucursal);

        var resultado = await _service.CambiarEstadoAsync(10, true);

        Assert.NotNull(resultado);
        Assert.True(resultado!.Activa);
        _repoMock.Verify(r => r.Update(It.IsAny<Sucursal>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NoModificaEstadoOperativo()
    {
        var sucursal = new Sucursal
        {
            Id = 11,
            Codigo = "TGU-01",
            Nombre = "Centro",
            ZonaHoraria = "America/Tegucigalpa",
            Activa = false
        };
        _repoMock.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(sucursal);
        _repoMock.Setup(r => r.ExisteCodigoAsync("TGU-02", 11)).ReturnsAsync(false);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.UpdateAsync(11, new UpdateSucursalDto
        {
            Codigo = "tgu-02",
            Nombre = "Centro Actualizada",
            ZonaHoraria = "America/Tegucigalpa"
        });

        Assert.NotNull(resultado);
        Assert.False(sucursal.Activa);
        Assert.Equal("TGU-02", sucursal.Codigo);
    }

    [Fact]
    public async Task DeleteAsync_AplicaSoftDeleteAuditado()
    {
        var sucursal = new Sucursal
        {
            Id = 12,
            Codigo = "TGU-03",
            Nombre = "Norte",
            ZonaHoraria = "America/Tegucigalpa",
            Activa = true
        };
        _repoMock.Setup(r => r.GetByIdAsync(12)).ReturnsAsync(sucursal);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.DeleteAsync(12);

        Assert.True(resultado);
        Assert.False(sucursal.Activa);
        Assert.True(sucursal.Eliminado);
        Assert.Equal(7, sucursal.EliminadoPorUsuarioId);
        Assert.NotNull(sucursal.FechaEliminacion);
        _repoMock.Verify(r => r.Update(sucursal), Times.Once);
    }

    [Fact]
    public async Task BuscarAsync_AplicaPaginacionDefensiva()
    {
        _repoMock.Setup(r => r.BuscarAsync(null, null, null, 1, 100))
            .ReturnsAsync((new List<Sucursal>(), 0));

        var resultado = await _service.BuscarAsync(new SucursalFiltroDto
        {
            Pagina = 0,
            TamanoPagina = 1000
        });

        Assert.Equal(1, resultado.Pagina);
        Assert.Equal(100, resultado.TamanoPagina);
        Assert.Equal(0, resultado.TotalPaginas);
    }
}
