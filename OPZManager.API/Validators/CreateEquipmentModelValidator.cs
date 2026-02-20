using FluentValidation;
using OPZManager.API.DTOs.Equipment;

namespace OPZManager.API.Validators
{
    public class CreateEquipmentModelValidator : AbstractValidator<CreateEquipmentModelDto>
    {
        public CreateEquipmentModelValidator()
        {
            RuleFor(x => x.ManufacturerId)
                .GreaterThan(0).WithMessage("Producent jest wymagany.");

            RuleFor(x => x.TypeId)
                .GreaterThan(0).WithMessage("Typ sprzętu jest wymagany.");

            RuleFor(x => x.ModelName)
                .NotEmpty().WithMessage("Nazwa modelu jest wymagana.")
                .MaximumLength(200).WithMessage("Nazwa modelu nie może przekraczać 200 znaków.");
        }
    }
}
