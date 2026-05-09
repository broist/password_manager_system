using PasswordManagerSystem.Client.Models.Companies;

namespace PasswordManagerSystem.Client.Services.Api;

public interface ICompaniesService
{
    Task<IReadOnlyList<CompanyResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CompanyResponse?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<CompanyResponse?> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default);
    Task<CompanyResponse?> UpdateAsync(long id, UpdateCompanyRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}

public sealed class CompaniesService : ICompaniesService
{
    private readonly IApiClient _apiClient;

    public CompaniesService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IReadOnlyList<CompanyResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _apiClient
            .GetAsync<List<CompanyResponse>>("api/Companies", cancellationToken)
            .ConfigureAwait(false);

        return list ?? new List<CompanyResponse>();
    }

    public Task<CompanyResponse?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => _apiClient.GetAsync<CompanyResponse>($"api/Companies/{id}", cancellationToken);

    public Task<CompanyResponse?> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
        => _apiClient.PostAsync<CreateCompanyRequest, CompanyResponse>("api/Companies", request, cancellationToken);

    public Task<CompanyResponse?> UpdateAsync(long id, UpdateCompanyRequest request, CancellationToken cancellationToken = default)
        => _apiClient.PutAsync<UpdateCompanyRequest, CompanyResponse>($"api/Companies/{id}", request, cancellationToken);

    public Task DeleteAsync(long id, CancellationToken cancellationToken = default)
        => _apiClient.DeleteAsync($"api/Companies/{id}", cancellationToken);
}
