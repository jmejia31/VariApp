using InventoryApp.Application.DTOs;
using InventoryApp.Application.Validators;
using Xunit;

namespace InventoryApp.Tests;

public class CategoriaValidatorTests
{
    private readonly CreateCategoriaValidator _validator = new();

    [Fact]
    public void Categoria_Valida_No_Genera_Errores()
    {
        var dto = new CreateCategoriaDto { Nombre = "Periféricos", Descripcion = "Mouse, teclados, etc." };
        var resultado = _validator.Validate(dto);
        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Categoria_Sin_Nombre_Genera_Error()
    {
        var dto = new CreateCategoriaDto { Nombre = "" };
        var resultado = _validator.Validate(dto);
        Assert.False(resultado.IsValid);
    }
}
