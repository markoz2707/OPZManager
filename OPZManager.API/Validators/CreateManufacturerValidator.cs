using FluentValidation;
using OPZManager.API.DTOs.Equipment;

namespace OPZManager.API.Validators
{
    public class CreateManufacturerValidator : AbstractValidator<CreateManufacturerDto>
    {
        public CreateManufacturerValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Nazwa producenta jest wymagana.")
                .MaximumLength(100).WithMessage("Nazwa producenta nie może przekraczać 100 znaków.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Opis nie może przekraczać 500 znaków.");
        }
    }
}
