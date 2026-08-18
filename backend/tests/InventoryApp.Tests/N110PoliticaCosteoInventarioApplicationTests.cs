using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N110PoliticaCosteoInventarioApplicationTests
{
    [Fact]
    public async Task Cambiar_mismo_metodo_es_idempotente_y_no_crea_historial_artificial()
    {
        var vigente = PoliticaCosteoInventario.Crear(
            1,
            MetodoCosteoInventario.PromedioPonderado,
            DateTime.UtcNow.AddDays(-10),
            "Política inicial");
        var (service, repository, _) = CrearServicio(vigente);

        var result = await service.CambiarAsync(new CambiarPoliticaCosteoInventarioDto
        {
            Metodo = MetodoCosteoInventario.PromedioPonderado,
            Motivo = "Solicitud repetida"
        });

        Assert.Equal(MetodoCosteoInventario.PromedioPonderado, result.Metodo);
        Assert.True(result.EstaVigente);
        repository.Verify(x => x.AddAsync(It.IsAny<PoliticaCosteoInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Cambiar_metodo_cierra_vigente_y_abre_nueva_version_sin_reescribir_historia()
    {
        var vigente = PoliticaCosteoInventario.Crear(
            1,
            MetodoCosteoInventario.PromedioPonderado,
            DateTime.UtcNow.AddDays(-10),
            "Política inicial");
        var (service, repository, capturada) = CrearServicio(vigente);

        var result = await service.CambiarAsync(new CambiarPoliticaCosteoInventarioDto
        {
            Metodo = MetodoCosteoInventario.FIFO,
            Motivo = "Adopción FIFO empresarial"
        });

        Assert.False(vigente.EstaVigente);
        Assert.NotNull(vigente.VigenteHastaUtc);
        Assert.Equal(MetodoCosteoInventario.FIFO, result.Metodo);
        Assert.Equal("Adopción FIFO empresarial", capturada.Value!.Motivo);
        Assert.True(capturada.Value.EstaVigente);
        repository.Verify(x => x.AddAsync(It.IsAny<PoliticaCosteoInventario>()), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Historial_rechaza_rango_invertido_antes_de_consultar_persistencia()
    {
        var (service, repository, _) = CrearServicio(null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetHistorialAsync(
            new PoliticaCosteoInventarioQueryDto
            {
                DesdeUtc = DateTime.UtcNow,
                HastaUtc = DateTime.UtcNow.AddDays(-1)
            }));

        repository.Verify(x => x.GetHistorialAsync(
            It.IsAny<int>(),
            It.IsAny<PoliticaCosteoInventarioQueryDto>()), Times.Never);
    }

    private static (
        PoliticaCosteoInventarioService Service,
        Mock<IPoliticaCosteoInventarioRepository> Repository,
        CapturaPolitica Capturada) CrearServicio(PoliticaCosteoInventario? vigente)
    {
        var repository = new Mock<IPoliticaCosteoInventarioRepository>();
        repository
            .Setup(x => x.GetVigenteAsync(1, It.IsAny<bool>()))
            .ReturnsAsync(vigente);
        var capturada = new CapturaPolitica();
        repository
            .Setup(x => x.AddAsync(It.IsAny<PoliticaCosteoInventario>()))
            .Callback<PoliticaCosteoInventario>(x => capturada.Value = x)
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var empresas = new Mock<IEmpresaConfiguracionRepository>();
        empresas.Setup(x => x.GetActivaAsync()).ReturnsAsync(new EmpresaConfiguracion { Id = 1, Activa = true });

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("qa-costeo");

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(operation => operation());

        return (
            new PoliticaCosteoInventarioService(repository.Object, empresas.Object, currentUser.Object, unitOfWork.Object),
            repository,
            capturada);
    }

    private sealed class CapturaPolitica
    {
        public PoliticaCosteoInventario? Value { get; set; }
    }
}
