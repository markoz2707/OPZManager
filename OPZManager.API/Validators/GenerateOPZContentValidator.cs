using FluentValidation;
using OPZManager.API.DTOs.OPZ;

namespace OPZManager.API.Validators
{
    public class GenerateOPZContentValidator : AbstractValidator<GenerateOPZContentRequestDto>
    {
        public GenerateOPZContentValidator()
        {
            RuleFor(x => x.EquipmentModelIds)
                .NotEmpty().WithMessage("Należy wybrać co najmniej jeden model sprzętu.");

            RuleFor(x => x.EquipmentType)
                .NotEmpty().WithMessage("Typ sprzętu jest wymagany.");
        }
    }

    public class GenerateOPZPdfValidator : AbstractValidator<GenerateOPZPdfRequestDto>
    {
        public GenerateOPZPdfValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Treść dokumentu jest wymagana.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tytuł dokumentu jest wymagany.");
        }
    }
}
