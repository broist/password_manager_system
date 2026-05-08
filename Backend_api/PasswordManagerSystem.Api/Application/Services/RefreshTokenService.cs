using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PasswordManagerSystem.Api.Application.Interfaces;
using PasswordManagerSystem.Api.Domain.Entities;
using PasswordManagerSystem.Api.Infrastructure.Data;

namespace PasswordManagerSystem.Api.Application.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public RefreshTokenService(
        AppDbContext dbContext,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<string> CreateRefreshTokenAsync(User user, string? createdByIp)
    {
        var now = DateTime.UtcNow;

        var activeTokens = await _dbContext.RefreshTokens
            .Where(x =>
                x.UserId == user.Id &&
                x.RevokedAt == null &&
                x.ExpiresAt > now)
            .ToListAsync();

        foreach (var activeToken in activeTokens)
        {
            activeToken.RevokedAt = now;
            activeToken.RevokedByIp = createdByIp;
        }

        var refreshToken = GenerateSecureToken();
        var tokenHash = HashRefreshToken(refreshToken);

        var refreshTokenDays = int.Parse(
            _configuration["Jwt:RefreshTokenDays"] ?? "1"
        );

        var entity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = now.AddDays(refreshTokenDays),
            RevokedAt = null,
            ReplacedByTokenHash = null,
            CreatedByIp = createdByIp,
            CreatedAt = now
        };

        _dbContext.RefreshTokens.Add(entity);
        await _dbContext.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<RefreshToken?> GetActiveRefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var tokenHash = HashRefreshToken(refreshToken);

        var token = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

        if (token is null)
        {
            return null;
        }

        if (!token.IsActive)
        {
            return null;
        }

        return token;
    }

    public async Task RevokeRefreshTokenAsync(
        RefreshToken refreshToken,
        string? revokedByIp,
        string? replacedByTokenHash = null)
    {
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = revokedByIp;
        refreshToken.ReplacedByTokenHash = replacedByTokenHash;

        await _dbContext.SaveChangesAsync();
    }

    public string HashRefreshToken(string refreshToken)
    {
        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var hashBytes = SHA256.HashData(bytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(randomBytes);
    }
}