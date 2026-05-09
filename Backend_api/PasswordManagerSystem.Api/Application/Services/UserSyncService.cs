using Microsoft.EntityFrameworkCore;
using PasswordManagerSystem.Api.Application.DTOs;
using PasswordManagerSystem.Api.Application.Interfaces;
using PasswordManagerSystem.Api.Domain.Entities;
using PasswordManagerSystem.Api.Infrastructure.Data;

namespace PasswordManagerSystem.Api.Application.Services;

public class UserSyncService : IUserSyncService
{
    private readonly AppDbContext _dbContext;

    public UserSyncService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User> SyncUserAsync(AdUserResult adUser, Role resolvedRole)
    {
        var now = DateTime.UtcNow;
        var normalizedUsername = adUser.AdUsername.Trim();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.AdUsername == normalizedUsername);

        if (user is null)
        {
            user = new User
            {
                AdUsername = normalizedUsername,
                FirstLoginAt = now,
                CreatedAt = now
            };

            _dbContext.Users.Add(user);
        }

        user.DisplayName = adUser.DisplayName;
        user.Email = adUser.Email;
        user.RoleId = resolvedRole.Id;
        user.IsActive = true;
        user.LastLoginAt = now;
        user.RoleSyncedAt = now;
        user.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        return user;
    }
}