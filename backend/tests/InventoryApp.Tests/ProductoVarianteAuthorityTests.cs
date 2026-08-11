using InventoryApp.Application.Mappings;
using InventoryApp.Domain.Entities;
using Xunit;

namespace InventoryApp.Tests;

public sealed class ProductoVarianteAuthorityTests
{
    [Fact]
    public void ProductoMapper_UsaExclusivamenteVarianteParaInventarioEconomiaYDimensiones()
    {
        var marca = new Marca { Id = 11, Nombre = "Marca Variante" };
        var modelo = new Modelo { Id = 12, MarcaId = 11, Nombre = "Modelo Variante" };
        var color = new Color { Id = 13, Nombre = "Negro" };
        var talla = new Talla { Id = 14, Nombre = "M" };
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Producto",
            Marca = "LEGACY INCORRECTA",
            Modelo = "LEGACY INCORRECTO",
            MarcaId = 91,
            ModeloId = 92,
            ColorId = 93,
            TallaId = 94,
            Cantidad = 999,
            Costo = 999m,
            Precio = 999m,
            UmbralStockBajo = 999,
            Activo = true
        };
        producto.Variantes.Add(new ProductoVariante
        {
            Id = 2,
            ProductoId = 1,
            MarcaId = 11,
            Marca = marca,
            ModeloId = 12,
            Modelo = modelo,
            ColorId = 13,
            Color = color,
            TallaId = 14,
            Talla = talla,
            Sku = "SKU-N03",
            CodigoBarras = "123456789",
            Cantidad = 7,
            Costo = 10m,
            Precio = 20m,
            UmbralStockBajo = 2,
            Activo = true
        });

        var dto = ProductoMapper.ToDto(producto);

        Assert.Equal(7, dto.Cantidad);
        Assert.Equal(10m, dto.Costo);
        Assert.Equal(20m, dto.Precio);
        Assert.Equal(2, dto.UmbralStockBajo);
        Assert.Equal(11, dto.MarcaId);
        Assert.Equal(12, dto.ModeloId);
        Assert.Equal(13, dto.ColorId);
        Assert.Equal(14, dto.TallaId);
        Assert.Equal("Marca Variante", dto.Marca);
        Assert.Equal("Modelo Variante", dto.Modelo);
    }
}
