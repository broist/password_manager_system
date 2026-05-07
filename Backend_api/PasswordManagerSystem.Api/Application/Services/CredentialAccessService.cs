using Microsoft.EntityFrameworkCore;
using PasswordManagerSystem.Api.Application.Interfaces;
using PasswordManagerSystem.Api.Infrastructure.Data;

namespace PasswordManagerSystem.Api.Application.Services;

public class CredentialAccessService : ICredentialAccessService
{
    private readonly AppDbContext _dbContext;

    public CredentialAccessService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> CanViewCredentialAsync(long credentialId, long userId, long roleId)
    {
        return await HasPermissionAsync(
            credentialId,
            userId,
            roleId,
            permissionSelector: PermissionType.View
        );
    }

    public async Task<bool> CanWriteCredentialAsync(long credentialId, long userId, long roleId)
    {
        return await HasPermissionAsync(
            credentialId,
            userId,
            roleId,
            permissionSelector: PermissionType.Write
        );
    }

    public async Task<bool> CanDeleteCredentialAsync(long credentialId, long userId, long roleId)
    {
        return await HasPermissionAsync(
            credentialId,
            userId,
            roleId,
            permissionSelector: PermissionType.Delete
        );
    }

    private async Task<bool> HasPermissionAsync(
        long credentialId,
        long userId,
        long roleId,
        PermissionType permissionSelector)
    {
        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == roleId && x.IsActive);

        if (role is null)
        {
            return false;
        }

        if (role.Name == "ITAdmin")
        {
            return true;
        }

        var now = DateTime.UtcNow;

        var userRule = await _dbContext.CredentialAccesses
            .AsNoTracking()
            .Where(x =>
                x.CredentialId == credentialId &&
                x.UserId == userId &&
                (x.ExpiresAt == null || x.ExpiresAt > now))
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        if (userRule is not null)
        {
            return permissionSelector switch
            {
                PermissionType.View => userRule.CanView,
                PermissionType.Write => userRule.CanWrite,
                PermissionType.Delete => userRule.CanDelete,
                _ => false
            };
        }

        var roleRule = await _dbContext.CredentialAccesses
            .AsNoTracking()
            .Where(x =>
                x.CredentialId == credentialId &&
                x.RoleId == roleId &&
                (x.ExpiresAt == null || x.ExpiresAt > now))
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        if (roleRule is not null)
        {
            return permissionSelector switch
            {
                PermissionType.View => roleRule.CanView,
                PermissionType.Write => roleRule.CanWrite,
                PermissionType.Delete => roleRule.CanDelete,
                _ => false
            };
        }

        return false;
    }

    private enum PermissionType
    {
        View,
        Write,
        Delete
    }
}