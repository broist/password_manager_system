using System.DirectoryServices.Protocols;
using System.Net;
using PasswordManagerSystem.Api.Application.DTOs;
using PasswordManagerSystem.Api.Application.Interfaces;

namespace PasswordManagerSystem.Api.Infrastructure.Authentication;

public class LdapAuthenticationService : IAdAuthenticationService
{
    private readonly LdapOptions _options;

    public LdapAuthenticationService(IConfiguration configuration)
    {
        _options = configuration
            .GetSection("Ldap")
            .Get<LdapOptions>()
            ?? throw new InvalidOperationException("LDAP configuration is missing.");
    }

    public Task<AdUserResult?> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult<AdUserResult?>(null);
        }

        var samAccountName = ExtractSamAccountName(username);
        var bindUsername = BuildBindUsername(samAccountName);

        try
        {
            using var connection = CreateConnection();

            connection.Bind(new NetworkCredential(bindUsername, password));

            var user = SearchUser(connection, samAccountName);

            return Task.FromResult(user);
        }
        catch (LdapException)
        {
            return Task.FromResult<AdUserResult?>(null);
        }
        catch (DirectoryOperationException)
        {
            return Task.FromResult<AdUserResult?>(null);
        }
    }

    private LdapConnection CreateConnection()
    {
        var identifier = new LdapDirectoryIdentifier(
            _options.Server,
            _options.Port
        );

        var connection = new LdapConnection(identifier)
        {
            AuthType = AuthType.Negotiate,
            Timeout = TimeSpan.FromSeconds(10)
        };

        connection.SessionOptions.ProtocolVersion = 3;

        if (_options.UseSsl)
        {
            connection.SessionOptions.SecureSocketLayer = true;
        }

        return connection;
    }

    private AdUserResult? SearchUser(LdapConnection connection, string samAccountName)
    {
        var escapedSamAccountName = EscapeLdapFilterValue(samAccountName);
        var filter = string.Format(_options.UserSearchFilter, escapedSamAccountName);

        var request = new SearchRequest(
            _options.BaseDn,
            filter,
            SearchScope.Subtree,
            "displayName",
            "mail",
            "sAMAccountName",
            _options.GroupAttribute
        );

        var response = (SearchResponse)connection.SendRequest(request);

        if (response.Entries.Count == 0)
        {
            return null;
        }

        var entry = response.Entries[0];

        var displayName = GetStringAttribute(entry, "displayName") ?? samAccountName;
        var email = GetStringAttribute(entry, "mail");

        var groups = GetStringAttributes(entry, _options.GroupAttribute)
            .Select(ExtractCommonName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        return new AdUserResult
        {
            AdUsername = $"{_options.Domain}\\{samAccountName}",
            DisplayName = displayName,
            Email = email,
            Groups = groups
        };
    }

    private string BuildBindUsername(string samAccountName)
    {
        if (string.IsNullOrWhiteSpace(_options.Domain))
        {
            return samAccountName;
        }

        return $"{_options.Domain}\\{samAccountName}";
    }

    private static string ExtractSamAccountName(string username)
    {
        var trimmed = username.Trim();

        if (trimmed.Contains('\\'))
        {
            return trimmed.Split('\\', StringSplitOptions.RemoveEmptyEntries).Last();
        }

        if (trimmed.Contains('@'))
        {
            return trimmed.Split('@', StringSplitOptions.RemoveEmptyEntries).First();
        }

        return trimmed;
    }

    private static string? GetStringAttribute(SearchResultEntry entry, string attributeName)
    {
        if (!entry.Attributes.Contains(attributeName))
        {
            return null;
        }

        var values = entry.Attributes[attributeName];

        if (values.Count == 0)
        {
            return null;
        }

        return values[0]?.ToString();
    }

    private static IEnumerable<string> GetStringAttributes(SearchResultEntry entry, string attributeName)
    {
        if (!entry.Attributes.Contains(attributeName))
        {
            return Enumerable.Empty<string>();
        }

        return entry.Attributes[attributeName]
            .GetValues(typeof(string))
            .Cast<string>();
    }

    private static string ExtractCommonName(string distinguishedName)
    {
        const string prefix = "CN=";

        if (!distinguishedName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return distinguishedName;
        }

        var commaIndex = distinguishedName.IndexOf(',');

        if (commaIndex <= 3)
        {
            return distinguishedName[3..];
        }

        return distinguishedName[3..commaIndex];
    }

    private static string EscapeLdapFilterValue(string value)
    {
        return value
            .Replace(@"\", @"\5c")
            .Replace("*", @"\2a")
            .Replace("(", @"\28")
            .Replace(")", @"\29")
            .Replace("\0", @"\00");
    }
}