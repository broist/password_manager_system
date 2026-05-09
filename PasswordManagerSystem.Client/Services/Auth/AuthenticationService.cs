using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PasswordManagerSystem.Client.Models.Auth;
using PasswordManagerSystem.Client.Services.Api;

namespace PasswordManagerSystem.Client.Services.Auth;

public interface IAuthenticationService : ITokenRefresher
{
    /// <summary>
    /// Bejelentkezés. Sikeres login esetén a token bundle-t menti és a LoginResponse-t adja vissza.
    /// Hiba esetén ApiException-t dob (vagy specifikusabbat).
    /// </summary>
    Task<LoginResponse> LoginAsync(string username, string password, CancellationToken cancellationToken);

    /// <summary>
    /// Kijelentkezés. Megpróbálja a backendet is értesíteni, de hiba esetén is törli a tokent.
    /// </summary>
    Task LogoutAsync(CancellationToken cancellationToken);
}

public sealed class AuthenticationService : IAuthenticationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        HttpClient httpClient,
        ITokenStore tokenStore,
        ILogger<AuthenticationService> logger)
    {
        _httpClient = httpClient;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var request = new LoginRequest
        {
            Username = username,
            Password = password
        };

        var response = await _httpClient
            .PostAsJsonAsync("api/Auth/login", request, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            string? message = null;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("message", out var m))
                {
                    message = m.GetString();
                }
            }
            catch
            {
                // ignore
            }

            throw new ApiException(
                response.StatusCode,
                message ?? "Bejelentkezés sikertelen.",
                null,
                raw);
        }

        var loginResponse = await response.Content
            .ReadFromJsonAsync<LoginResponse>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (loginResponse is null)
        {
            throw new InvalidOperationException("Login response was empty.");
        }

        var bundle = new TokenBundle(
            loginResponse.AccessToken,
            loginResponse.RefreshToken,
            DateTime.UtcNow.AddMinutes(loginResponse.ExpiresInMinutes));

        _tokenStore.Save(bundle);

        return loginResponse;
    }

    public async Task<bool> RefreshAsync(CancellationToken cancellationToken)
    {
        var current = _tokenStore.Load();

        if (current is null || string.IsNullOrWhiteSpace(current.RefreshToken))
        {
            return false;
        }

        var request = new RefreshTokenRequest
        {
            RefreshToken = current.RefreshToken
        };

        try
        {
            var response = await _httpClient
                .PostAsJsonAsync("api/Auth/refresh", request, JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Refresh token rejected: {StatusCode}", response.StatusCode);
                _tokenStore.Clear();
                return false;
            }

            var refreshResponse = await response.Content
                .ReadFromJsonAsync<RefreshTokenResponse>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (refreshResponse is null)
            {
                return false;
            }

            var newBundle = new TokenBundle(
                refreshResponse.AccessToken,
                refreshResponse.RefreshToken,
                DateTime.UtcNow.AddMinutes(refreshResponse.ExpiresInMinutes));

            _tokenStore.Save(newBundle);

            _logger.LogInformation("Token refreshed.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed.");
            return false;
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        var current = _tokenStore.Load();

        if (current is not null && !string.IsNullOrWhiteSpace(current.RefreshToken))
        {
            try
            {
                var request = new LogoutRequest
                {
                    RefreshToken = current.RefreshToken
                };

                await _httpClient
                    .PostAsJsonAsync("api/Auth/logout", request, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Logout API call failed; clearing token locally.");
            }
        }

        _tokenStore.Clear();
    }
}
