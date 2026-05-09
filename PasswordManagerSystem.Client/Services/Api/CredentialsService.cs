using PasswordManagerSystem.Client.Models.Credentials;

namespace PasswordManagerSystem.Client.Services.Api;

public interface ICredentialsService
{
    Task<IReadOnlyList<CredentialListItemResponse>> GetAllAsync(
        long? companyId = null,
        CancellationToken cancellationToken = default);

    Task<CredentialDetailResponse?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<CredentialDetailResponse?> CreateAsync(
        CreateCredentialRequest request,
        CancellationToken cancellationToken = default);

    Task<CredentialDetailResponse?> UpdateAsync(
        long id,
        UpdateCredentialRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    Task<RevealUsernameResponse?> RevealUsernameAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<RevealPasswordResponse?> RevealPasswordAsync(
        long id,
        CancellationToken cancellationToken = default);
}

public sealed class CredentialsService : ICredentialsService
{
    private readonly IApiClient _apiClient;

    public CredentialsService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IReadOnlyList<CredentialListItemResponse>> GetAllAsync(
        long? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var path = companyId.HasValue
            ? $"api/Credentials?companyId={companyId.Value}"
            : "api/Credentials";

        var list = await _apiClient
            .GetAsync<List<CredentialListItemResponse>>(path, cancellationToken)
            .ConfigureAwait(false);

        return list ?? new List<CredentialListItemResponse>();
    }

    public Task<CredentialDetailResponse?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => _apiClient.GetAsync<CredentialDetailResponse>($"api/Credentials/{id}", cancellationToken);

    public Task<CredentialDetailResponse?> CreateAsync(CreateCredentialRequest request, CancellationToken cancellationToken = default)
        => _apiClient.PostAsync<CreateCredentialRequest, CredentialDetailResponse>("api/Credentials", request, cancellationToken);

    public Task<CredentialDetailResponse?> UpdateAsync(long id, UpdateCredentialRequest request, CancellationToken cancellationToken = default)
        => _apiClient.PutAsync<UpdateCredentialRequest, CredentialDetailResponse>($"api/Credentials/{id}", request, cancellationToken);

    public Task DeleteAsync(long id, CancellationToken cancellationToken = default)
        => _apiClient.DeleteAsync($"api/Credentials/{id}", cancellationToken);

    public Task<RevealUsernameResponse?> RevealUsernameAsync(long id, CancellationToken cancellationToken = default)
        => _apiClient.PostNoBodyAsync<RevealUsernameResponse>($"api/Credentials/{id}/reveal-username", cancellationToken);

    public Task<RevealPasswordResponse?> RevealPasswordAsync(long id, CancellationToken cancellationToken = default)
        => _apiClient.PostNoBodyAsync<RevealPasswordResponse>($"api/Credentials/{id}/reveal-password", cancellationToken);
}
