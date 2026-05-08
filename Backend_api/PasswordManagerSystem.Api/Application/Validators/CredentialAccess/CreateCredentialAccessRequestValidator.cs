using FluentValidation;
using PasswordManagerSystem.Api.Application.DTOs.CredentialAccess;

namespace PasswordManagerSystem.Api.Application.Validators.CredentialAccess;

public class CreateCredentialAccessRequestValidator : AbstractValidator<CreateCredentialAccessRequest>
{
    public CreateCredentialAccessRequestValidator()
    {
        RuleFor(x => x.CredentialId)
            .GreaterThan(0)
            .WithMessage("CredentialId is required.");

        RuleFor(x => x)
            .Must(x => x.RoleId.HasValue ^ x.UserId.HasValue)
            .WithMessage("Exactly one of RoleId or UserId must be provided.");

        RuleFor(x => x)
            .Must(x => x.CanView || x.CanWrite || x.CanDelete)
            .WithMessage("At least one permission must be enabled.");

        RuleFor(x => x.RoleId)
            .GreaterThan(0)
            .When(x => x.RoleId.HasValue)
            .WithMessage("RoleId must be greater than 0.");

        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .When(x => x.UserId.HasValue)
            .WithMessage("UserId must be greater than 0.");

        RuleFor(x => x.ExpiresAt)
            .Must(x => x == null || x > DateTime.UtcNow)
            .WithMessage("ExpiresAt must be in the future.");
    }
}