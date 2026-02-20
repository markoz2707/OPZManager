using FluentValidation;
using OPZManager.API.DTOs.Admin;

namespace OPZManager.API.Validators
{
    public class CreateTrainingDataValidator : AbstractValidator<CreateTrainingDataDto>
    {
        public CreateTrainingDataValidator()
        {
            RuleFor(x => x.Question)
                .NotEmpty().WithMessage("Pytanie jest wymagane.");

            RuleFor(x => x.Answer)
                .NotEmpty().WithMessage("Odpowiedź jest wymagana.");

            RuleFor(x => x.DataType)
                .NotEmpty().WithMessage("Typ danych jest wymagany.")
                .MaximumLength(50).WithMessage("Typ danych nie może przekraczać 50 znaków.");
        }
    }
}
