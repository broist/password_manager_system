using Microsoft.EntityFrameworkCore;
using PasswordManagerSystem.Api.Application.Interfaces;
using PasswordManagerSystem.Api.Domain.Entities;
using PasswordManagerSystem.Api.Infrastructure.Data;

namespace PasswordManagerSystem.Api.Application.Services;

public class RoleResolverService : IRoleResolverService
{
    private readonly AppDbContext _dbContext;

    public RoleResolverService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Role?> ResolveHighestRoleAsync(IEnumerable<string> adGroups)
    {
        var normalizedGroups = adGroups
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (normalizedGroups.Count == 0)
        {
            return null;
        }

        return await _dbContext.Roles
            .Where(role =>
                role.IsActive &&
                normalizedGroups.Contains(role.AdGroupName.ToLower()))
            .OrderByDescending(role => role.Level)
            .FirstOrDefaultAsync();
    }
}