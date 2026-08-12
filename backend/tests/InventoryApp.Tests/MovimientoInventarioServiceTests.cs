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
    public async Task GetFilteredAsync_Incluye_Imagen_Principal_Y_Origen_Tipado()
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
            Producto = producto,
            Tipo = TipoMovimientoInventario.Entrada,
            Cantidad = 2,
            StockAnterior = 0,
            StockNuevo = 2,
            ReferenciaTipo = "Compra",
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
                [movimiento.Id] = new(movimiento.Id, 5, null, null)
            });
        var service = new MovimientoInventarioService(repository.Object);

        var resultado = await service.GetFilteredAsync(null, null, null, null);

        var dto = resultado.Single();
        Assert.Equal("https://res.cloudinary.com/demo/image/upload/teclado.webp", dto.ProductoImagenPrincipalUrl);
        Assert.Equal("Compra", dto.OrigenTipo);
        Assert.Equal(5, dto.OrigenId);
        Assert.Equal(5, dto.CompraId);
        Assert.Null(dto.VentaId);
        Assert.Null(dto.ConsumoInsumoId);
        Assert.Equal("Compra", dto.ReferenciaTipo);
        Assert.Equal(5, dto.ReferenciaId);
    }
}
