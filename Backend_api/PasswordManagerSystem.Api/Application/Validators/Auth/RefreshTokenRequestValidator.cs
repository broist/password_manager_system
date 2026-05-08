using FluentValidation;
using PasswordManagerSystem.Api.Application.DTOs.Auth;

namespace PasswordManagerSystem.Api.Application.Validators.Auth;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required.")
            .MaximumLength(2000)
            .WithMessage("Refresh token must not exceed 2000 characters.");
    }
}