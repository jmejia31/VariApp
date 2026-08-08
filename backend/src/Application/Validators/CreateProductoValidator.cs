using FluentValidation;
using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Validators;

public class CreateProductoValidator : AbstractValidator<CreateProductoDto>
{
    public CreateProductoValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.").MaximumLength(150);
        RuleFor(x => x)
            .Must(x => x.MarcaId.HasValue || !string.IsNullOrWhiteSpace(x.Marca))
            .WithMessage("La marca es obligatoria.");
        RuleFor(x => x)
            .Must(x => x.ModeloId.HasValue || !string.IsNullOrWhiteSpace(x.Modelo))
            .WithMessage("El modelo es obligatorio.");
        RuleFor(x => x.Marca).MaximumLength(100);
        RuleFor(x => x.Modelo).MaximumLength(100);
        RuleFor(x => x.Cantidad).GreaterThanOrEqualTo(0).WithMessage("La cantidad no puede ser negativa.");
        RuleFor(x => x.Costo).GreaterThan(0).WithMessage("El costo debe ser mayor a 0.");
        RuleFor(x => x.Precio).GreaterThan(0).WithMessage("El precio debe ser mayor a 0.");
        RuleFor(x => x.UmbralStockBajo).GreaterThanOrEqualTo(0).WithMessage("El umbral de stock bajo no puede ser negativo.");

        RuleFor(x => x.Imagenes)
            .Must(imgs => imgs == null || imgs.Count <= ImagenValidationHelper.MaxImagenes)
            .WithMessage($"Un producto puede tener máximo {ImagenValidationHelper.MaxImagenes} fotos.");

        RuleForEach(x => x.Imagenes)
            .Must(ImagenValidationHelper.EsImagenValida)
            .WithMessage("Cada imagen debe ser JPG, JPEG, PNG o WEBP y pesar máximo 10 MB.");
    }
}
