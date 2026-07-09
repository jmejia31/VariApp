using InventoryApp.Application.DTOs;
using InventoryApp.Application.Validators;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class ProductoImagenValidationTests
{
    private readonly CreateProductoValidator _validator = new();

    private static IFormFile CrearImagenFalsa(string contentType = "image/jpeg", long tamanoBytes = 1024)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(tamanoBytes);
        mock.Setup(f => f.FileName).Returns("foto.jpg");
        return mock.Object;
    }

    private CreateProductoDto DtoBase(List<IFormFile>? imagenes = null) => new()
    {
        Nombre = "Mouse",
        Marca = "Logitech",
        Modelo = "M185",
        Cantidad = 10,
        Costo = 5,
        Precio = 12,
        Imagenes = imagenes
    };

    [Fact]
    public void Hasta_5_Imagenes_Es_Valido()
    {
        var imagenes = Enumerable.Range(0, 5).Select(_ => CrearImagenFalsa()).ToList();
        var resultado = _validator.Validate(DtoBase(imagenes));

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Mas_De_5_Imagenes_Es_Invalido()
    {
        var imagenes = Enumerable.Range(0, 6).Select(_ => CrearImagenFalsa()).ToList();
        var resultado = _validator.Validate(DtoBase(imagenes));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Imagenes");
    }

    [Fact]
    public void Tipo_De_Archivo_Invalido_Es_Rechazado()
    {
        var imagenes = new List<IFormFile> { CrearImagenFalsa(contentType: "application/pdf") };
        var resultado = _validator.Validate(DtoBase(imagenes));

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Imagen_Muy_Pesada_Es_Rechazada()
    {
        var imagenes = new List<IFormFile> { CrearImagenFalsa(tamanoBytes: 10 * 1024 * 1024) };
        var resultado = _validator.Validate(DtoBase(imagenes));

        Assert.False(resultado.IsValid);
    }
}
