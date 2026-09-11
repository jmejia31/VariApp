using FluentValidation;
using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Validators;

public class UpdateClienteValidator : AbstractValidator<UpdateClienteDto>
{
    public UpdateClienteValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre del cliente es obligatorio.").MaximumLength(200);
        RuleFor(x => x.Correo).EmailAddress().When(x => !string.IsNullOrEmpty(x.Correo)).WithMessage("El correo no es válido.");
    }
}
