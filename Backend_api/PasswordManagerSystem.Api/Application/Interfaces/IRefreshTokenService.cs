using PasswordManagerSystem.Api.Domain.Entities;

namespace PasswordManagerSystem.Api.Application.Interfaces;

public interface IRefreshTokenService
{
    Task<string> CreateRefreshTokenAsync(User user, string? createdByIp);

    Task<RefreshToken?> GetActiveRefreshTokenAsync(string refreshToken);

    Task RevokeRefreshTokenAsync(
        RefreshToken refreshToken,
        string? revokedByIp,
        string? replacedByTokenHash = null
    );

    string HashRefreshToken(string refreshToken);
}