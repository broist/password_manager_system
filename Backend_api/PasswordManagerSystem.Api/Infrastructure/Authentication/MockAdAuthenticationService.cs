using PasswordManagerSystem.Api.Application.DTOs;
using PasswordManagerSystem.Api.Application.Interfaces;

namespace PasswordManagerSystem.Api.Infrastructure.Authentication;

public class MockAdAuthenticationService : IAdAuthenticationService
{
    public Task<AdUserResult?> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult<AdUserResult?>(null);
        }

        var normalizedUsername = username.Trim();

        if (password != "Test1234!")
        {
            return Task.FromResult<AdUserResult?>(null);
        }

        var groups = ResolveMockGroups(normalizedUsername);

        if (groups.Count == 0)
        {
            return Task.FromResult<AdUserResult?>(null);
        }

        var result = new AdUserResult
        {
            AdUsername = normalizedUsername,
            DisplayName = normalizedUsername,
            Email = null,
            Groups = groups
        };

        return Task.FromResult<AdUserResult?>(result);
    }

    private static List<string> ResolveMockGroups(string username)
    {
        var lowerUsername = username.ToLowerInvariant();

        if (lowerUsername.Contains("admin"))
        {
            return new List<string> { "erp_kp_itadm" };
        }

        if (lowerUsername.Contains("it"))
        {
            return new List<string> { "erp_kp_it" };
        }

        if (lowerUsername.Contains("consultant"))
        {
            return new List<string> { "erp_kp_erpconsultant" };
        }

        if (lowerUsername.Contains("support"))
        {
            return new List<string> { "erp_kp_erpsupport" };
        }

        return new List<string>();
    }
}