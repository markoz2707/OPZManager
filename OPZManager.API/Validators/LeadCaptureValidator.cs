using FluentValidation;
using OPZManager.API.DTOs.Public;

namespace OPZManager.API.Validators
{
    public class LeadCaptureValidator : AbstractValidator<LeadCaptureRequestDto>
    {
        public LeadCaptureValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Adres email jest wymagany.")
                .EmailAddress().WithMessage("Podaj prawidłowy adres email.")
                .MaximumLength(255);

            RuleFor(x => x.Source)
                .Must(s => s == "verification" || s == "generation")
                .WithMessage("Nieprawidłowe źródło.");
        }
    }
}
