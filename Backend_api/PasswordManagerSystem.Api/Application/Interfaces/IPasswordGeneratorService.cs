using PasswordManagerSystem.Api.Application.DTOs.PasswordGenerator;

namespace PasswordManagerSystem.Api.Application.Interfaces;

public interface IPasswordGeneratorService
{
    GeneratePasswordResponse Generate(GeneratePasswordRequest request);
}