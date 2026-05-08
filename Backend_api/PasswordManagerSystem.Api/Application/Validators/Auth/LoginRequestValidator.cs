using FluentValidation;
using PasswordManagerSystem.Api.Application.DTOs;

namespace PasswordManagerSystem.Api.Application.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required.")
            .MaximumLength(200)
            .WithMessage("Username must not exceed 200 characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MaximumLength(500)
            .WithMessage("Password must not exceed 500 characters.");
    }
}