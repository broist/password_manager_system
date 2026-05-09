using FluentValidation;
using PasswordManagerSystem.Api.Application.DTOs.PasswordGenerator;

namespace PasswordManagerSystem.Api.Application.Validators.PasswordGenerator;

public class GeneratePasswordRequestValidator : AbstractValidator<GeneratePasswordRequest>
{
    public GeneratePasswordRequestValidator()
    {
        RuleFor(x => x.Length)
            .InclusiveBetween(8, 128)
            .WithMessage("Password length must be between 8 and 128 characters.");

        RuleFor(x => x)
            .Must(x =>
                x.IncludeUppercase ||
                x.IncludeLowercase ||
                x.IncludeDigits ||
                x.IncludeSpecialCharacters)
            .WithMessage("At least one character set must be enabled.");

        RuleFor(x => x)
            .Must(x =>
                x.IncludeUppercase ||
                x.IncludeLowercase ||
                x.IncludeSpecialCharacters)
            .WithMessage("At least one non-digit character set must be enabled because the password cannot start with a digit.");
    }
}