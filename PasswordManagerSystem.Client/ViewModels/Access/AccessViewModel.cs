using System.Collections.ObjectModel;
using System.Net;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PasswordManagerSystem.Client.Models.Companies;
using PasswordManagerSystem.Client.Models.CredentialAccess;
using PasswordManagerSystem.Client.Models.Credentials;
using PasswordManagerSystem.Client.Services.Api;
using PasswordManagerSystem.Client.Services.Notifications;
using PasswordManagerSystem.Client.Services.Session;
using PasswordManagerSystem.Client.Views.Access;

namespace PasswordManagerSystem.Client.ViewModels.Access;

public sealed partial class AccessViewModel : ObservableObject
{
    private readonly ICompaniesService _companiesService;
    private readonly ICredentialsService _credentialsService;
    private readonly ICredentialAccessApiService _credentialAccessApiService;
    private readonly IUsersApiService _usersApiService;
    private readonly IToastService _toastService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<AccessViewModel> _logger;

    private readonly List<CredentialListItemResponse> _allVisibleCredentials = new();

    public AccessViewModel(
        ICompaniesService companiesService,
        ICredentialsService credentialsService,
        ICredentialAccessApiService credentialAccessApiService,
        IUsersApiService usersApiService,
        IToastService toastService,
        ISessionService sessionService,
        ILogger<AccessViewModel> logger)
    {
        _companiesService = companiesService;
        _credentialsService = credentialsService;
        _credentialAccessApiService = credentialAccessApiService;
        _usersApiService = usersApiService;
        _toastService = toastService;
        _sessionService = sessionService;
        _logger = logger;

        _ = InitializeAsync();
    }

    public ObservableCollection<CompanyResponse> Companies { get; } = new();

    public ObservableCollection<CredentialListItemResponse> Credentials { get; } = new();

    public ObservableCollection<AccessCredentialItemViewModel> CredentialItems { get; } = new();

    public ObservableCollection<CredentialAccessResponse> AccessRules { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedCompany))]
    private CompanyResponse? _selectedCompany;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedCredential))]
    private CredentialListItemResponse? _selectedCredential;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GrantTemporaryAccessCommand))]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public bool HasSelectedCompany => SelectedCompany is not null;

    public bool HasSelectedCredential => SelectedCredential is not null;

    public bool CanManageAccessRules => _sessionService.IsItAdmin || _sessionService.IsIt;

    public bool CanManageRoleAccess => _sessionService.IsItAdmin || _sessionService.IsIt;

    public bool CanManageUserAccess => _sessionService.IsItAdmin || _sessionService.IsIt;

    public bool CanGrantAdminRole => _sessionService.IsItAdmin;

    public bool CanGrantWriteDelete => _sessionService.IsItAdmin;

    public bool IsDelegatedItMode => _sessionService.IsIt && !_sessionService.IsItAdmin;

    partial void OnSelectedCompanyChanged(CompanyResponse? value)
    {
        _ = ApplyCompanyCredentialFilterAsync(value?.Id);
    }

    partial void OnSelectedCredentialChanged(CredentialListItemResponse? value)
    {
        _ = LoadAccessRulesAsync(value?.Id);
    }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            await LoadCompaniesInternalAsync();
            await LoadCredentialsInternalAsync();

            SelectedCompany ??= Companies.FirstOrDefault();
        }
        catch (ApiException ex)
        {
            HandleApiException(ex, "Hozzáférések betöltése sikertelen.");
            _logger.LogWarning(ex, "Access view initialization failed.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toastService.ShowError($"Váratlan hiba: {ex.Message}");
            _logger.LogError(ex, "Access view initialization unexpected error.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCompaniesInternalAsync()
    {
        var companies = await _companiesService.GetAllAsync();

        Companies.Clear();

        foreach (var company in companies
                     .Where(x => x.IsActive)
                     .OrderBy(x => x.Name))
        {
            Companies.Add(company);
        }
    }

    private async Task LoadCredentialsInternalAsync()
    {
        var credentials = await _credentialsService.GetAllAsync(null);

        _allVisibleCredentials.Clear();

        foreach (var credential in credentials
                     .Where(x => x.IsActive)
                     .OrderBy(x => x.CompanyName)
                     .ThenBy(x => x.Title))
        {
            _allVisibleCredentials.Add(credential);
        }
    }

    private async Task ApplyCompanyCredentialFilterAsync(long? companyId)
    {
        Credentials.Clear();
        CredentialItems.Clear();
        AccessRules.Clear();
        SelectedCredential = null;

        if (companyId is null)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var filteredCredentials = _allVisibleCredentials
                .Where(x => x.CompanyId == companyId.Value)
                .OrderBy(x => x.Title)
                .ToList();

            foreach (var credential in filteredCredentials)
            {
                Credentials.Add(credential);

                var item = new AccessCredentialItemViewModel(credential);
                CredentialItems.Add(item);

                await ReloadAccessRulesForItemAsync(item);
            }

            SelectedCredential = Credentials.FirstOrDefault();
        }
        catch (ApiException ex)
        {
            HandleApiException(ex, "Hozzáférési szabályok betöltése sikertelen.");
            _logger.LogWarning(ex, "Company credential access rules load failed.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toastService.ShowError($"Váratlan hiba: {ex.Message}");
            _logger.LogError(ex, "Company credential access rules load unexpected error.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ReloadAccessRulesForItemAsync(AccessCredentialItemViewModel item)
    {
        item.ClearAccessRules();

        var rules = await _credentialAccessApiService.GetByCredentialAsync(item.CredentialId);

        foreach (var rule in rules
                     .OrderBy(x => x.RoleName is null)
                     .ThenBy(x => x.RoleName)
                     .ThenBy(x => x.AdUsername))
        {
            item.AddAccessRule(rule);
        }
    }

    private async Task LoadAccessRulesAsync(long? credentialId)
    {
        AccessRules.Clear();

        if (credentialId is null)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var rules = await _credentialAccessApiService.GetByCredentialAsync(credentialId.Value);

            foreach (var rule in rules
                         .OrderBy(x => x.RoleName is null)
                         .ThenBy(x => x.RoleName)
                         .ThenBy(x => x.AdUsername))
            {
                AccessRules.Add(rule);
            }
        }
        catch (ApiException ex)
        {
            HandleApiException(ex, "Hozzáférési szabályok betöltése sikertelen.");
            _logger.LogWarning(ex, "Credential access rules load failed.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toastService.ShowError($"Váratlan hiba: {ex.Message}");
            _logger.LogError(ex, "Credential access rules load unexpected error.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanGrantTemporaryAccess(AccessCredentialItemViewModel? item)
    {
        return !IsLoading &&
               CanManageUserAccess &&
               item is not null;
    }

    [RelayCommand(CanExecute = nameof(CanGrantTemporaryAccess))]
    private async Task GrantTemporaryAccessAsync(AccessCredentialItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var users = await _usersApiService.GetActiveUsersAsync();

            if (users.Count == 0)
            {
                _toastService.ShowWarning("Nincs aktív felhasználó, akinek hozzáférést lehetne adni.");
                return;
            }

            var owner = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(x => x.IsActive)
                ?? Application.Current.MainWindow;

            var window = new GrantTemporaryAccessWindow(item.Title, users)
            {
                Owner = owner
            };

            var result = window.ShowDialog();

            if (result != true || !window.IsConfirmed || window.SelectedUser is null)
            {
                return;
            }

            var request = new CreateCredentialAccessRequest
            {
                CredentialId = item.CredentialId,
                RoleId = null,
                UserId = window.SelectedUser.Id,
                CanView = true,
                CanWrite = false,
                CanDelete = false,
                ExpiresAt = DateTime.UtcNow.AddHours(window.SelectedHours)
            };

            await _credentialAccessApiService.CreateAsync(request);

            await ReloadAccessRulesForItemAsync(item);

            _toastService.ShowSuccess("Alkalmi hozzáférés létrehozva.");
        }
        catch (ApiException ex)
        {
            HandleApiException(ex, "Alkalmi hozzáférés létrehozása sikertelen.");
            _logger.LogWarning(ex, "Temporary access grant failed.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toastService.ShowError($"Váratlan hiba: {ex.Message}");
            _logger.LogError(ex, "Temporary access grant unexpected error.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void HandleApiException(ApiException ex, string messagePrefix)
    {
        ErrorMessage = ex.Message;

        if (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            _toastService.ShowWarning("A munkamenet lejárt — jelentkezz be újra.");
            return;
        }

        if (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            _toastService.ShowWarning($"{messagePrefix} Nincs jogosultság.");
            return;
        }

        _toastService.ShowError($"{messagePrefix} {ex.Message}");
    }
}

public sealed partial class AccessCredentialItemViewModel : ObservableObject
{
    public AccessCredentialItemViewModel(CredentialListItemResponse credential)
    {
        Credential = credential;
    }

    public CredentialListItemResponse Credential { get; }

    public ObservableCollection<CredentialAccessResponse> AccessRules { get; } = new();

    public ObservableCollection<CredentialAccessResponse> TemporaryAccessRules { get; } = new();

    public long CredentialId => Credential.Id;

    public string Title => Credential.Title;

    public string CompanyName => Credential.CompanyName;

    public string? ConnectionValue => Credential.ConnectionValue;

    public string? Notes => Credential.Notes;

    public bool HasTemporaryAccessRules => TemporaryAccessRules.Count > 0;

    public string RoleAccessSummary
    {
        get
        {
            var activeRoleRules = AccessRules
                .Where(x =>
                    x.RoleId.HasValue &&
                    x.CanView &&
                    !string.IsNullOrWhiteSpace(x.RoleName) &&
                    !IsExpired(x))
                .Select(x => x.RoleName!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (activeRoleRules.Count == 0)
            {
                return "Nincs szerepkör-alapú hozzáférés";
            }

            var minimumRole = activeRoleRules
                .OrderBy(GetRoleRank)
                .First();

            return $"Minimum szerepkör: {minimumRole}";
        }
    }

    public void AddAccessRule(CredentialAccessResponse rule)
    {
        AccessRules.Add(rule);

        if (rule.UserId.HasValue && rule.CanView && !IsExpired(rule))
        {
            TemporaryAccessRules.Add(rule);
        }

        OnPropertyChanged(nameof(HasTemporaryAccessRules));
        OnPropertyChanged(nameof(RoleAccessSummary));
    }

    public void ClearAccessRules()
    {
        AccessRules.Clear();
        TemporaryAccessRules.Clear();

        OnPropertyChanged(nameof(HasTemporaryAccessRules));
        OnPropertyChanged(nameof(RoleAccessSummary));
    }

    private static bool IsExpired(CredentialAccessResponse rule)
    {
        return rule.ExpiresAt.HasValue &&
               rule.ExpiresAt.Value <= DateTime.UtcNow;
    }

    private static int GetRoleRank(string roleName)
    {
        return roleName.Trim().ToUpperInvariant() switch
        {
            "SUPPORT" => 1,
            "CONSULTANT" => 2,
            "IT" => 3,
            "ITADMIN" => 4,
            _ => 999
        };
    }
}