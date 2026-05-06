using PasswordManagerSystem.Api.Domain.Entities;

namespace PasswordManagerSystem.Api.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, Role role);
}