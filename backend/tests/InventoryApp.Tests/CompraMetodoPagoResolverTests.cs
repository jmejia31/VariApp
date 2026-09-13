using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;
using CatalogoMetodoPago = InventoryApp.Domain.Entities.Catalogos.MetodoPago;

namespace InventoryApp.Tests;

public class CompraMetodoPagoResolverTests
{
    [Fact]
    public async Task ResolverAsync_UsaCatalogoActivoComoAutoridad()
    {
        var repo = new Mock<ICompraRepository>();
        var catalogo = new CatalogoMetodoPago
        {
            Id = 77,
            Codigo = "TRANSFERENCIA",
            Nombre = "Transferencia bancaria",
            Activo = true
        };

        repo.Setup(r => r.GetMetodoPagoPorCodigoONombreAsync("TRANSFERENCIA"))
            .ReturnsAsync(catalogo);

        var resultado = await CompraMetodoPagoResolver.ResolverAsync(repo.Object, "  TRANSFERENCIA  ");

        Assert.Same(catalogo, resultado);
        repo.Verify(r => r.GetMetodoPagoPorCodigoONombreAsync("TRANSFERENCIA"), Times.Once);
    }

    [Fact]
    public async Task ResolverAsync_FallaCerrado_SiMetodoNoExiste()
    {
        var repo = new Mock<ICompraRepository>();
        repo.Setup(r => r.GetMetodoPagoPorCodigoONombreAsync("CRIPTO"))
            .ReturnsAsync((CatalogoMetodoPago?)null);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => CompraMetodoPagoResolver.ResolverAsync(repo.Object, "CRIPTO"));

        Assert.Contains("no existe en el catálogo", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DerivarLegacy_MetodoAdministrableNuevo_ProyectaOtroSinPerderAutoridadRelacional()
    {
        var catalogo = new CatalogoMetodoPago
        {
            Id = 99,
            Codigo = "QR_EMPRESARIAL",
            Nombre = "QR Empresarial",
            Activo = true
        };

        var legacy = CompraMetodoPagoResolver.DerivarLegacy(catalogo);

        Assert.Equal(MetodoPago.Otro, legacy);
        Assert.Equal(99, catalogo.Id);
    }
}
