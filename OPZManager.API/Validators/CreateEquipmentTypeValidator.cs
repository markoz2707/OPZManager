using FluentValidation;
using OPZManager.API.DTOs.Equipment;

namespace OPZManager.API.Validators
{
    public class CreateEquipmentTypeValidator : AbstractValidator<CreateEquipmentTypeDto>
    {
        public CreateEquipmentTypeValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Nazwa typu sprzętu jest wymagana.")
                .MaximumLength(100).WithMessage("Nazwa typu sprzętu nie może przekraczać 100 znaków.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Opis nie może przekraczać 500 znaków.");
        }
    }
}
