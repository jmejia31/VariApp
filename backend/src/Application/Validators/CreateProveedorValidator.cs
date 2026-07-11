using FluentValidation;
using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Validators;

public class CreateProveedorValidator : AbstractValidator<CreateProveedorDto>
{
    public CreateProveedorValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre del proveedor es obligatorio.").MaximumLength(200);
        RuleFor(x => x.Correo).EmailAddress().When(x => !string.IsNullOrEmpty(x.Correo)).WithMessage("El correo no es válido.");
    }
}
