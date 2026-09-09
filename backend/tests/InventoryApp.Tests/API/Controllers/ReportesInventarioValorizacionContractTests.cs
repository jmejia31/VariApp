using InventoryApp.API.Controllers;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryApp.Tests.API.Controllers;

public sealed class ReportesInventarioValorizacionContractTests
{
    [Fact]
    public async Task GetResumen_SinFinanzasVer_CensuraSinConsultarFinanzas()
    {
        var finanzas = new Mock<IFinanzasService>(MockBehavior.Strict);
        var permisos = new Mock<IPermisoService>();
        permisos.Setup(x => x.TienePermisoAsync(ModuloSistema.Finanzas, AccionPermiso.Ver))
            .ReturnsAsync(false);
        var controller = new ReportesInventarioValorizacionController(finanzas.Object, permisos.Object);

        var result = await controller.GetResumen();

        Assert.IsType<OkObjectResult>(result);
        finanzas.Verify(x => x.GetResumenAsync(), Times.Never);
        Assert.All(typeof(ReporteInventarioValorizacionResumenDto).GetProperties(), property =>
            Assert.Equal(typeof(decimal?), property.PropertyType));
    }
}
