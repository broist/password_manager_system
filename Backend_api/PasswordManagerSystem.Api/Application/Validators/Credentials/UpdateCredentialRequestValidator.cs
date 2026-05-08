using FluentValidation;
using PasswordManagerSystem.Api.Application.DTOs.Credentials;

namespace PasswordManagerSystem.Api.Application.Validators.Credentials;

public class UpdateCredentialRequestValidator : AbstractValidator<UpdateCredentialRequest>
{
    public UpdateCredentialRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Credential title is required.")
            .MaximumLength(200)
            .WithMessage("Credential title must not exceed 200 characters.");

        RuleFor(x => x.Username)
            .MaximumLength(500)
            .WithMessage("Username must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Username));

        RuleFor(x => x.Password)
            .MaximumLength(1000)
            .WithMessage("Password must not exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Password));

        RuleFor(x => x.ConnectionValue)
            .MaximumLength(1000)
            .WithMessage("Connection value must not exceed 1000 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(4000)
            .WithMessage("Notes must not exceed 4000 characters.");
    }
}