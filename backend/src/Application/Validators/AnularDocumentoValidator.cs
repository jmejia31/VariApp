using FluentValidation;
using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Validators;

public class AnularDocumentoValidator : AbstractValidator<AnularDocumentoDto>
{
    public AnularDocumentoValidator()
    {
        RuleFor(x => x.MotivoAnulacion).NotEmpty().WithMessage("El motivo de anulación es obligatorio.");
    }
}
