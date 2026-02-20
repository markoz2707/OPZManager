using FluentValidation;
using OPZManager.API.DTOs.Auth;

namespace OPZManager.API.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Nazwa użytkownika jest wymagana.")
                .MaximumLength(100).WithMessage("Nazwa użytkownika nie może przekraczać 100 znaków.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Hasło jest wymagane.");
        }
    }
}
