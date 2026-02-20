using FluentValidation;
using OPZManager.API.DTOs.Auth;

namespace OPZManager.API.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Nazwa użytkownika jest wymagana.")
                .MinimumLength(3).WithMessage("Nazwa użytkownika musi mieć co najmniej 3 znaki.")
                .MaximumLength(100).WithMessage("Nazwa użytkownika nie może przekraczać 100 znaków.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Adres email jest wymagany.")
                .EmailAddress().WithMessage("Podano nieprawidłowy adres email.")
                .MaximumLength(255).WithMessage("Adres email nie może przekraczać 255 znaków.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Hasło jest wymagane.")
                .MinimumLength(6).WithMessage("Hasło musi mieć co najmniej 6 znaków.");
        }
    }
}
