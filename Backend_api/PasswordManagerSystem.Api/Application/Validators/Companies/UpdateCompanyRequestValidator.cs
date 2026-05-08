using FluentValidation;
using PasswordManagerSystem.Api.Application.DTOs.Companies;

namespace PasswordManagerSystem.Api.Application.Validators.Companies;

public class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Company name is required.")
            .MaximumLength(200)
            .WithMessage("Company name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Company description must not exceed 1000 characters.");
    }
}