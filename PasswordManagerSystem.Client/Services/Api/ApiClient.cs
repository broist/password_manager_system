using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using PasswordManagerSystem.Client.Models.Common;

namespace PasswordManagerSystem.Client.Services.Api;

/// <summary>
/// Egységes API hívási réteg. Minden HTTP hívás átmegy ezen,
/// így itt egy helyen tudunk hibakezelést, JSON serializálást és logolást csinálni.
/// </summary>
public interface IApiClient
{
    Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default);

    Task<TResponse?> PostAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken cancellationToken = default);

    Task PostAsync<TRequest>(
        string path,
        TRequest body,
        CancellationToken cancellationToken = default);

    Task<TResponse?> PutAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    Task<TResponse?> PostNoBodyAsync<TResponse>(
        string path,
        CancellationToken cancellationToken = default);
}

public sealed class ApiClient : IApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);
        return await ProcessResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient
            .PostAsJsonAsync(path, body, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        return await ProcessResponseAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task PostAsync<TRequest>(
        string path,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient
            .PostAsJsonAsync(path, body, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient
            .PutAsJsonAsync(path, body, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        return await ProcessResponseAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync(path, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResponse?> PostNoBodyAsync<TResponse>(
        string path,
        CancellationToken cancellationToken = default)
    {
        using var content = new StringContent(string.Empty);

        var response = await _httpClient
            .PostAsync(path, content, cancellationToken)
            .ConfigureAwait(false);

        return await ProcessResponseAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<T?> ProcessResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NoContent ||
            response.Content.Headers.ContentLength == 0)
        {
            return default;
        }

        return await response.Content
            .ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var status = response.StatusCode;
        var rawBody = string.Empty;

        try
        {
            rawBody = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }

        ApiErrorResponse? parsed = null;

        if (!string.IsNullOrWhiteSpace(rawBody))
        {
            try
            {
                parsed = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody, JsonOptions);
            }
            catch
            {
                // ignore parse errors
            }
        }

        throw new ApiException(status, parsed?.Message ?? response.ReasonPhrase ?? "API error", parsed?.Errors, rawBody);
    }
}

/// <summary>
/// Tipizált API hiba. A ViewModel rétegben tudunk rá szépen reagálni.
/// </summary>
public sealed class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public Dictionary<string, string[]>? ValidationErrors { get; }
    public string? RawBody { get; }

    public ApiException(
        HttpStatusCode statusCode,
        string message,
        Dictionary<string, string[]>? validationErrors,
        string? rawBody)
        : base(message)
    {
        StatusCode = statusCode;
        ValidationErrors = validationErrors;
        RawBody = rawBody;
    }
}
