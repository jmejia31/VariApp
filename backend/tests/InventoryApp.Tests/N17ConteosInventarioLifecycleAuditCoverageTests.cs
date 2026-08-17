using InventoryApp.API.Controllers;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N17ConteosInventarioLifecycleAuditCoverageTests
{
    [Fact]
    public async Task Create_RegistraAuditoriaCrearConReferencia()
    {
        var service = new Mock<IConteoInventarioService>();
        var auditoria = new Mock<IAuditoriaService>();
        var input = new CreateConteoInventarioDto { AlmacenId = 3, ProductoVarianteIds = { 9 } };
        var creado = new ConteoInventarioDto
        {
            Id = 41,
            Numero = "CNT-41",
            AlmacenId = 3,
            Estado = EstadoConteoInventario.Borrador
        };
        service.Setup(x => x.CreateAsync(input)).ReturnsAsync(creado);
        var controller = new ConteosInventarioController(service.Object, auditoria.Object);

        var result = await controller.Create(input);

        Assert.IsType<CreatedAtActionResult>(result);
        VerificarAuditoria(auditoria, AccionPermiso.Crear, 41);
    }

    [Fact]
    public async Task Update_RegistraAuditoriaEditarConReferencia()
    {
        var service = new Mock<IConteoInventarioService>();
        var auditoria = new Mock<IAuditoriaService>();
        var input = new UpdateConteoInventarioDto { AlmacenId = 3, ProductoVarianteIds = { 9 } };
        var actualizado = new ConteoInventarioDto
        {
            Id = 42,
            Numero = "CNT-42",
            AlmacenId = 3,
            Estado = EstadoConteoInventario.Borrador
        };
        service.Setup(x => x.UpdateAsync(42, input)).ReturnsAsync(actualizado);
        var controller = new ConteosInventarioController(service.Object, auditoria.Object);

        var result = await controller.Update(42, input);

        Assert.IsType<OkObjectResult>(result);
        VerificarAuditoria(auditoria, AccionPermiso.Editar, 42);
    }

    [Fact]
    public async Task Iniciar_RegistraAuditoriaCambiarEstadoConReferencia()
    {
        var service = new Mock<IConteoInventarioService>();
        var auditoria = new Mock<IAuditoriaService>();
        var iniciado = new ConteoInventarioDto
        {
            Id = 43,
            Numero = "CNT-43",
            AlmacenId = 3,
            Estado = EstadoConteoInventario.EnProceso
        };
        service.Setup(x => x.IniciarAsync(43)).ReturnsAsync(iniciado);
        var controller = new ConteosInventarioController(service.Object, auditoria.Object);

        var result = await controller.Iniciar(43);

        Assert.IsType<OkObjectResult>(result);
        VerificarAuditoria(auditoria, AccionPermiso.CambiarEstado, 43);
    }

    [Fact]
    public async Task Aprobar_RegistraAuditoriaAprobarConReferencia()
    {
        var service = new Mock<IConteoInventarioService>();
        var auditoria = new Mock<IAuditoriaService>();
        var aprobado = new ConteoInventarioDto
        {
            Id = 44,
            Numero = "CNT-44",
            AlmacenId = 3,
            Estado = EstadoConteoInventario.Aprobado
        };
        service.Setup(x => x.AprobarAsync(44)).ReturnsAsync(aprobado);
        var controller = new ConteosInventarioController(service.Object, auditoria.Object);

        var result = await controller.Aprobar(44);

        Assert.IsType<OkObjectResult>(result);
        VerificarAuditoria(auditoria, AccionPermiso.Aprobar, 44);
    }

    private static void VerificarAuditoria(
        Mock<IAuditoriaService> auditoria,
        AccionPermiso accion,
        int referenciaId)
    {
        auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.MovimientosInventario,
            accion,
            It.IsAny<string>(),
            referenciaId,
            "ConteoInventario",
            null,
            It.IsAny<object>(),
            null,
            "Exito",
            null), Times.Once);
    }
}
