using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class MovimientoInventarioServiceTests
{
    [Fact]
    public async Task GetFilteredAsync_Incluye_Contexto_Empresarial_Completo_Y_Origen_Tipado()
    {
        var producto = new Producto { Id = 2, Nombre = "Teclado", Marca = "Logitech", Modelo = "K120" };
        producto.Imagenes.Add(new ProductoImagen
        {
            Id = 22,
            Url = "https://res.cloudinary.com/demo/image/upload/teclado.webp",
            EsPrincipal = true,
            Orden = 0
        });
        var movimiento = new MovimientoInventario
        {
            Id = 3,
            ProductoId = producto.Id,
            ProductoVarianteId = 12,
            AlmacenId = 4,
            UbicacionAlmacenId = 9,
            Producto = producto,
            Tipo = TipoMovimientoInventario.Entrada,
            Causa = CausaMovimientoInventario.AjusteManual,
            Cantidad = 2,
            StockAnterior = 0,
            StockNuevo = 2,
            CostoUnitario = 125.50m,
            PrecioUnitario = 180m,
            CorrelationId = "ajuste:2026-08-15:3",
            ReferenciaTipo = "AjusteInventario",
            ReferenciaId = 5
        };
        var repository = new Mock<IMovimientoInventarioRepository>();
        repository
            .Setup(r => r.GetFilteredAsync(null, null, null, null))
            .ReturnsAsync(new List<MovimientoInventario> { movimiento });
        repository
            .Setup(r => r.GetOrigenesTipadosAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new Dictionary<int, MovimientoInventarioOrigenPersistido>
            {
                [movimiento.Id] = new(movimiento.Id, null, null, null, 5)
            });
        var service = new MovimientoInventarioService(repository.Object);

        var resultado = await service.GetFilteredAsync(null, null, null, null);

        var dto = resultado.Single();
        Assert.Equal("https://res.cloudinary.com/demo/image/upload/teclado.webp", dto.ProductoImagenPrincipalUrl);
        Assert.Equal(12, dto.ProductoVarianteId);
        Assert.Equal(4, dto.AlmacenId);
        Assert.Equal(9, dto.UbicacionAlmacenId);
        Assert.Equal("AjusteManual", dto.Causa);
        Assert.Equal(125.50m, dto.CostoUnitario);
        Assert.Equal(180m, dto.PrecioUnitario);
        Assert.Equal("ajuste:2026-08-15:3", dto.CorrelationId);
        Assert.Equal("AjusteInventario", dto.OrigenTipo);
        Assert.Equal(5, dto.OrigenId);
        Assert.Equal(5, dto.AjusteInventarioId);
        Assert.Null(dto.CompraId);
        Assert.Null(dto.VentaId);
        Assert.Null(dto.ConsumoInsumoId);
        Assert.Equal("AjusteInventario", dto.ReferenciaTipo);
        Assert.Equal(5, dto.ReferenciaId);
    }
}
