using PasswordManagerSystem.Client.Models.Common;
using PasswordManagerSystem.Client.Models.PasswordGenerator;
using PasswordManagerSystem.Client.Models.CredentialAccess;
using PasswordManagerSystem.Client.Models.Audit;

namespace PasswordManagerSystem.Client.Services.Api;

public interface IPasswordGeneratorService
{
    Task<GeneratePasswordResponse?> GenerateAsync(
        GeneratePasswordRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class PasswordGeneratorService : IPasswordGeneratorService
{
    private readonly IApiClient _apiClient;

    public PasswordGeneratorService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<GeneratePasswordResponse?> GenerateAsync(
        GeneratePasswordRequest request,
        CancellationToken cancellationToken = default)
        => _apiClient.PostAsync<GeneratePasswordRequest, GeneratePasswordResponse>(
            "api/PasswordGenerator/generate",
            request,
            cancellationToken);
}

public interface IHealthService
{
    Task<HealthResponse?> GetHealthAsync(CancellationToken cancellationToken = default);
}

public sealed class HealthService : IHealthService
{
    private readonly IApiClient _apiClient;

    public HealthService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<HealthResponse?> GetHealthAsync(CancellationToken cancellationToken = default)
        => _apiClient.GetAsync<HealthResponse>("api/Health", cancellationToken);
}

public interface ICredentialAccessApiService
{
    Task<IReadOnlyList<CredentialAccessResponse>> GetByCredentialAsync(
        long credentialId,
        CancellationToken cancellationToken = default);

    Task<CredentialAccessResponse?> CreateAsync(
        CreateCredentialAccessRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}

public sealed class CredentialAccessApiService : ICredentialAccessApiService
{
    private readonly IApiClient _apiClient;

    public CredentialAccessApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IReadOnlyList<CredentialAccessResponse>> GetByCredentialAsync(
        long credentialId,
        CancellationToken cancellationToken = default)
    {
        var list = await _apiClient
            .GetAsync<List<CredentialAccessResponse>>(
                $"api/CredentialAccess/credential/{credentialId}",
                cancellationToken)
            .ConfigureAwait(false);

        return list ?? new List<CredentialAccessResponse>();
    }

    public Task<CredentialAccessResponse?> CreateAsync(
        CreateCredentialAccessRequest request,
        CancellationToken cancellationToken = default)
        => _apiClient.PostAsync<CreateCredentialAccessRequest, CredentialAccessResponse>(
            "api/CredentialAccess",
            request,
            cancellationToken);

    public Task DeleteAsync(long id, CancellationToken cancellationToken = default)
        => _apiClient.DeleteAsync($"api/CredentialAccess/{id}", cancellationToken);
}

public interface IAuditApiService
{
    Task<AuditChainVerificationResponse?> VerifyChainAsync(CancellationToken cancellationToken = default);
}

public sealed class AuditApiService : IAuditApiService
{
    private readonly IApiClient _apiClient;

    public AuditApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<AuditChainVerificationResponse?> VerifyChainAsync(CancellationToken cancellationToken = default)
        => _apiClient.GetAsync<AuditChainVerificationResponse>("api/Audit/verify-chain", cancellationToken);
}
