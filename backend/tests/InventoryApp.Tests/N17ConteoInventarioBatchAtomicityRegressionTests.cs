using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N17ConteoInventarioBatchAtomicityRegressionTests
{
    [Fact]
    public async Task CapturarLote_LineaAjena_NoMutaLineasValidasNiPersisteParcialmente()
    {
        var repository = new Mock<IConteoInventarioRepository>();
        var existencias = new Mock<IExistenciaVarianteRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(operation => operation());

        var detalleValido = new ConteoInventarioDetalle
        {
            Id = 41,
            ProductoVarianteId = 501,
            AlmacenId = 3
        };
        detalleValido.MaterializarSnapshot(12);

        var conteo = new ConteoInventario
        {
            Id = 18,
            Numero = "CNT-ATOMICO-18",
            Tipo = TipoConteoInventario.General,
            AlmacenId = 3,
            Detalles = new List<ConteoInventarioDetalle> { detalleValido }
        };
        conteo.Iniciar(7, DateTime.UtcNow);

        repository.Setup(x => x.GetByIdForUpdateAsync(18)).ReturnsAsync(conteo);
        var service = new ConteoInventarioService(
            repository.Object,
            existencias.Object,
            currentUser.Object,
            unitOfWork.Object);

        var lote = new CapturarConteoInventarioLoteDto
        {
            Lineas = new List<CapturaConteoInventarioLineaDto>
            {
                new() { DetalleId = 41, CantidadContada = 10 },
                new() { DetalleId = 999, CantidadContada = 5 }
            }
        };

        var error = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.CapturarLoteAsync(18, lote));

        Assert.Contains("no pertenecen al conteo", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(detalleValido.CantidadContada);
        Assert.Null(detalleValido.FechaConteo);
        Assert.Null(detalleValido.ContadoPorUsuarioId);
        repository.Verify(x => x.Update(It.IsAny<ConteoInventario>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }
}
