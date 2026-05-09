using PasswordManagerSystem.Api.Domain.Entities;

namespace PasswordManagerSystem.Api.Application.Interfaces;

public interface IRoleResolverService
{
    Task<Role?> ResolveHighestRoleAsync(IEnumerable<string> adGroups);
}