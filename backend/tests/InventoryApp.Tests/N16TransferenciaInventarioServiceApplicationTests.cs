using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N16TransferenciaInventarioServiceApplicationTests
{
    [Fact]
    public async Task Solicitar_ReintentoSobreSolicitada_EsIdempotenteYSinNuevaEscritura()
    {
        var transferencia = CrearTransferenciaSolicitada();
        var repository = new Mock<ITransferenciaInventarioRepository>();
        repository.Setup(x => x.GetByIdForUpdateAsync(transferencia.Id)).ReturnsAsync(transferencia);
        var service = CrearService(repository);

        var resultado = await service.SolicitarAsync(transferencia.Id);

        Assert.NotNull(resultado);
        Assert.Equal("Solicitada", resultado!.Estado);
        repository.Verify(x => x.Update(It.IsAny<TransferenciaInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Aprobar_RechazaDetalleDuplicadoAntesDeMutarDocumento()
    {
        var transferencia = CrearTransferenciaSolicitada();
        var detalleId = transferencia.Detalles.Single().Id;
        var repository = new Mock<ITransferenciaInventarioRepository>();
        repository.Setup(x => x.GetByIdForUpdateAsync(transferencia.Id)).ReturnsAsync(transferencia);
        var service = CrearService(repository);
        var dto = new AprobarTransferenciaInventarioDto
        {
            Detalles =
            {
                new AprobarTransferenciaInventarioDetalleDto { DetalleId = detalleId, CantidadAprobada = 2 },
                new AprobarTransferenciaInventarioDetalleDto { DetalleId = detalleId, CantidadAprobada = 2 }
            }
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.AprobarAsync(transferencia.Id, dto));

        Assert.Equal("Solicitada", transferencia.Estado.ToString());
        repository.Verify(x => x.Update(It.IsAny<TransferenciaInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    private static TransferenciaInventarioService CrearService(Mock<ITransferenciaInventarioRepository> repository)
    {
        var almacenes = new Mock<IAlmacenRepository>();
        var variantes = new Mock<IProductoVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.EstaAutenticado).Returns(true);
        currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("qa-n16");

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(operation => operation());

        return new TransferenciaInventarioService(
            repository.Object,
            almacenes.Object,
            variantes.Object,
            currentUser.Object,
            unitOfWork.Object);
    }

    private static TransferenciaInventario CrearTransferenciaSolicitada()
    {
        var detalle = new TransferenciaInventarioDetalle
        {
            Id = 41,
            ProductoVarianteId = 91
        };
        detalle.EstablecerCantidadSolicitada(3);

        var transferencia = new TransferenciaInventario
        {
            Id = 12,
            Numero = "TRF-N16-12",
            AlmacenOrigenId = 1,
            AlmacenDestinoId = 2,
            Detalles = new List<TransferenciaInventarioDetalle> { detalle }
        };
        transferencia.Solicitar(7, DateTime.UtcNow);
        return transferencia;
    }
}
