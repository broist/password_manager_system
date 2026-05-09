using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using System.IO;

namespace PasswordManagerSystem.Client.Services.Auth;

/// <summary>
/// HTTP message handler, ami:
///   1) minden kimenő kérésre rárakja az aktuális Bearer tokent,
///   2) ha 401-et kapunk, megpróbál refresh-elni és újraküldi a kérést,
///   3) ha a refresh is bukik, eldobja a session-t (a SessionService figyel rá).
/// </summary>
public sealed class AuthHeaderHandler : DelegatingHandler
{
    private readonly ITokenStore _tokenStore;
    private readonly ITokenRefresher _tokenRefresher;
    private readonly ILogger<AuthHeaderHandler> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthHeaderHandler(
        ITokenStore tokenStore,
        ITokenRefresher tokenRefresher,
        ILogger<AuthHeaderHandler> logger)
    {
        _tokenStore = tokenStore;
        _tokenRefresher = tokenRefresher;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Az Auth/login és Auth/refresh végpontokra NE rakjunk Bearer tokent,
        // különben a backend a régi tokent fogja látni és összezavarodik.
        var isAuthEndpoint = request.RequestUri?.AbsolutePath
            .Contains("/api/Auth/", StringComparison.OrdinalIgnoreCase) == true;

        if (!isAuthEndpoint)
        {
            AttachToken(request);
        }

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // 401 + nem auth végpont => próbáljunk refresh-elni egyszer
        if (response.StatusCode == HttpStatusCode.Unauthorized && !isAuthEndpoint)
        {
            _logger.LogInformation("401 received from {Url}, attempting token refresh.", request.RequestUri);

            var refreshed = await TryRefreshAsync(cancellationToken).ConfigureAwait(false);

            if (refreshed)
            {
                response.Dispose();

                // Új token => kérés újraküldése
                var retryRequest = await CloneRequestAsync(request).ConfigureAwait(false);
                AttachToken(retryRequest);

                response = await base.SendAsync(retryRequest, cancellationToken).ConfigureAwait(false);
            }
        }

        return response;
    }

    private void AttachToken(HttpRequestMessage request)
    {
        var bundle = _tokenStore.Load();

        if (bundle is not null && !string.IsNullOrWhiteSpace(bundle.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bundle.AccessToken);
        }
    }

    private async Task<bool> TryRefreshAsync(CancellationToken cancellationToken)
    {
        // Csak egy szál próbáljon refresh-elni egyszerre, a többi várjon az eredményre.
        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await _tokenRefresher.RefreshAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version
        };

        if (original.Content is not null)
        {
            var ms = new MemoryStream();
            await original.Content.CopyToAsync(ms).ConfigureAwait(false);
            ms.Position = 0;

            var content = new StreamContent(ms);

            foreach (var header in original.Content.Headers)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Content = content;
        }

        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var prop in original.Options)
        {
            ((IDictionary<string, object?>)clone.Options)[prop.Key] = prop.Value;
        }

        return clone;
    }
}

/// <summary>
/// A token refresh felelőse. Külön interfész, mert az AuthenticationService
/// fogja megvalósítani, és el kell kerülnünk a circular dependency-t a HttpClient
/// építésénél.
/// </summary>
public interface ITokenRefresher
{
    Task<bool> RefreshAsync(CancellationToken cancellationToken);
}
