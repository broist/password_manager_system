using PasswordManagerSystem.Client.Models.Common;
using PasswordManagerSystem.Client.Models.PasswordGenerator;
using PasswordManagerSystem.Client.Models.CredentialAccess;
using PasswordManagerSystem.Client.Models.Audit;
using PasswordManagerSystem.Client.Models.Users;
using PasswordManagerSystem.Client.Models.CredentialUsage;

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
	
	public interface ICredentialUsageApiService
{
    Task<IReadOnlyList<ActiveCredentialUsageResponse>> GetActiveAsync(
        long credentialId,
        CancellationToken cancellationToken = default);

    Task<StartCredentialUsageResponse?> StartAsync(
        StartCredentialUsageRequest request,
        CancellationToken cancellationToken = default);

    Task EndAsync(
        long usageSessionId,
        EndCredentialUsageRequest request,
        CancellationToken cancellationToken = default);
}

	public sealed class CredentialUsageApiService : ICredentialUsageApiService
	{
		private readonly IApiClient _apiClient;

		public CredentialUsageApiService(IApiClient apiClient)
		{
			_apiClient = apiClient;
		}

		public async Task<IReadOnlyList<ActiveCredentialUsageResponse>> GetActiveAsync(
			long credentialId,
			CancellationToken cancellationToken = default)
		{
			var result = await _apiClient
				.GetAsync<List<ActiveCredentialUsageResponse>>(
					$"api/CredentialUsage/active/{credentialId}",
					cancellationToken)
				.ConfigureAwait(false);

			return result ?? new List<ActiveCredentialUsageResponse>();
		}

		public Task<StartCredentialUsageResponse?> StartAsync(
			StartCredentialUsageRequest request,
			CancellationToken cancellationToken = default)
			=> _apiClient.PostAsync<StartCredentialUsageRequest, StartCredentialUsageResponse>(
				"api/CredentialUsage/start",
				request,
				cancellationToken);

		public async Task EndAsync(
			long usageSessionId,
			EndCredentialUsageRequest request,
			CancellationToken cancellationToken = default)
		{
			await _apiClient.PostAsync<EndCredentialUsageRequest, object>(
					$"api/CredentialUsage/end/{usageSessionId}",
					request,
					cancellationToken)
				.ConfigureAwait(false);
		}
	}

	public interface IAuditApiService
	{
		Task<AuditChainVerificationResponse?> VerifyChainAsync(
			CancellationToken cancellationToken = default);

		Task<AuditLogListResponse?> GetLogsAsync(
			int take = 200,
			string? action = null,
			string? adUsername = null,
			bool? success = null,
			CancellationToken cancellationToken = default);
	}

	public sealed class AuditApiService : IAuditApiService
	{
		private readonly IApiClient _apiClient;

		public AuditApiService(IApiClient apiClient)
		{
			_apiClient = apiClient;
		}

		public Task<AuditChainVerificationResponse?> VerifyChainAsync(
			CancellationToken cancellationToken = default)
			=> _apiClient.GetAsync<AuditChainVerificationResponse>(
				"api/Audit/verify-chain",
				cancellationToken);

		public Task<AuditLogListResponse?> GetLogsAsync(
			int take = 200,
			string? action = null,
			string? adUsername = null,
			bool? success = null,
			CancellationToken cancellationToken = default)
		{
			var queryParts = new List<string>
			{
				$"take={take}"
			};

			if (!string.IsNullOrWhiteSpace(action))
			{
				queryParts.Add($"action={Uri.EscapeDataString(action)}");
			}

			if (!string.IsNullOrWhiteSpace(adUsername))
			{
				queryParts.Add($"adUsername={Uri.EscapeDataString(adUsername)}");
			}

			if (success.HasValue)
			{
				queryParts.Add($"success={success.Value.ToString().ToLowerInvariant()}");
			}

			var query = string.Join("&", queryParts);

			return _apiClient.GetAsync<AuditLogListResponse>(
				$"api/Audit/logs?{query}",
				cancellationToken);
		}
	}

	public interface IUsersApiService
	{
		Task<IReadOnlyList<ActiveUserResponse>> GetActiveUsersAsync(
			CancellationToken cancellationToken = default);
	}

	public sealed class UsersApiService : IUsersApiService
	{
		private readonly IApiClient _apiClient;

		public UsersApiService(IApiClient apiClient)
		{
			_apiClient = apiClient;
		}

		public async Task<IReadOnlyList<ActiveUserResponse>> GetActiveUsersAsync(
			CancellationToken cancellationToken = default)
		{
			var list = await _apiClient
				.GetAsync<List<ActiveUserResponse>>(
					"api/Users/active",
					cancellationToken)
				.ConfigureAwait(false);

			return list ?? new List<ActiveUserResponse>();
		}
	}