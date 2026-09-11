using FluentValidation;
using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.NombreUsuario).NotEmpty().WithMessage("El usuario es obligatorio.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}
