using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PasswordManagerSystem.Client.Configuration;
using PasswordManagerSystem.Client.Models.Companies;
using PasswordManagerSystem.Client.Models.Credentials;
using PasswordManagerSystem.Client.Services.Api;
using PasswordManagerSystem.Client.Services.Clipboard;
using PasswordManagerSystem.Client.Services.Notifications;
using PasswordManagerSystem.Client.Services.Session;
using PasswordManagerSystem.Client.Views.Credentials;

namespace PasswordManagerSystem.Client.ViewModels.Credentials;

public sealed partial class CredentialListViewModel : ObservableObject
{
    private readonly ICompaniesService _companiesService;
    private readonly ICredentialsService _credentialsService;
    private readonly Func<long, string, CredentialEditorViewModel> _credentialEditorFactory;
    private readonly IClipboardService _clipboardService;
    private readonly IToastService _toastService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<CredentialListViewModel> _logger;
    private readonly SessionSettings _sessionSettings;

    private readonly List<CredentialListItemResponse> _allCredentials = new();

    private CancellationTokenSource? _autoMaskCts;

    public CredentialListViewModel(
        ICompaniesService companiesService,
        ICredentialsService credentialsService,
        Func<long, string, CredentialEditorViewModel> credentialEditorFactory,
        IClipboardService clipboardService,
        IToastService toastService,
        ISessionService sessionService,
        IOptions<AppSettings> options,
        ILogger<CredentialListViewModel> logger)
    {
        _companiesService = companiesService;
        _credentialsService = credentialsService;
        _credentialEditorFactory = credentialEditorFactory;
        _clipboardService = clipboardService;
        _toastService = toastService;
        _sessionService = sessionService;
        _logger = logger;
        _sessionSettings = options.Value.Session;

        _ = InitializeAsync();
    }

    public ObservableCollection<CompanyResponse> Companies { get; } = new();

    public ObservableCollection<CredentialListItemResponse> Credentials { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateCredentialCommand))]
    private CompanyResponse? _selectedCompany;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedCredential))]
    [NotifyCanExecuteChangedFor(nameof(RevealUsernameCommand))]
    [NotifyCanExecuteChangedFor(nameof(RevealPasswordCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyUsernameCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyPasswordCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenConnectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartEditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCredentialCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveEditCommand))]
    private CredentialListItemResponse? _selectedCredential;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateCredentialCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartEditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCredentialCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveEditCommand))]
    private bool _isLoading;

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private string? _revealedUsername;

    [ObservableProperty]
    private string? _revealedPassword;

    [ObservableProperty]
    private bool _isPasswordRevealed;

    [ObservableProperty]
    private bool _isUsernameRevealed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartEditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCredentialCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveEditCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelEditCommand))]
    private bool _isEditMode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveEditCommand))]
    private string _editTitle = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveEditCommand))]
    private string _editUsername = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveEditCommand))]
    private string _editPassword = string.Empty;

    [ObservableProperty]
    private string? _editConnectionValue;

    [ObservableProperty]
    private string? _editNotes;

    [ObservableProperty]
    private string? _editErrorMessage;

    public bool HasSelectedCredential => SelectedCredential is not null;

    public bool CanCreateCredentials => _sessionService.CanCreateCredentials;

    public bool CanEditDeleteCredentials => _sessionService.CanCreateCredentials;

    private async Task InitializeAsync()
    {
        await LoadCompaniesAsync();
    }

    private bool CanCreateCredential()
    {
        return !IsLoading &&
               CanCreateCredentials &&
               SelectedCompany is not null;
    }

    [RelayCommand(CanExecute = nameof(CanCreateCredential))]
    private async Task CreateCredentialAsync()
    {
        if (SelectedCompany is null)
        {
            _toastService.ShowWarning("Előbb válassz ki egy céget.");
            return;
        }

        try
        {
            var editorViewModel = _credentialEditorFactory(
                SelectedCompany.Id,
                SelectedCompany.Name
            );

            var owner = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(x => x.IsActive)
                ?? Application.Current.MainWindow;

            var window = new CredentialEditorWindow(editorViewModel)
            {
                Owner = owner
            };

            var result = window.ShowDialog();

            if (result != true)
            {
                return;
            }

            await LoadCredentialsAsync(SelectedCompany.Id);

            if (editorViewModel.CreatedCredential is not null)
            {
                SelectedCredential = Credentials
                    .FirstOrDefault(x => x.Id == editorViewModel.CreatedCredential.Id);
            }

            _toastService.ShowSuccess("Új bejegyzés létrehozva.");
        }
        catch (ApiException ex)
        {
            _toastService.ShowError($"Bejegyzés létrehozása sikertelen: {ex.Message}");
            _logger.LogWarning(ex, "Create credential failed.");
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Váratlan hiba: {ex.Message}");
            _logger.LogError(ex, "Create credential unexpected error.");
        }
    }

    [RelayCommand]
    private async Task LoadCompaniesAsync()
    {
        IsLoading = true;

        try
        {
            var companies = await _companiesService.GetAllAsync();

            Companies.Clear();

            foreach (var company in companies.Where(c => c.IsActive).OrderBy(c => c.Name))
            {
                Companies.Add(company);
            }

            if (SelectedCompany is null && Companies.Count > 0)
            {
                SelectedCompany = Companies[0];
            }
            else
            {
                await LoadCredentialsAsync(null);
            }
        }
        catch (ApiException ex)
        {
            _toastService.ShowError($"Cégek betöltése sikertelen: {ex.Message}");
            _logger.LogError(ex, "Cégek betöltési hiba.");
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Hálózati hiba: {ex.Message}");
            _logger.LogError(ex, "Cégek betöltési kivétel.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedCompanyChanged(CompanyResponse? value)
    {
        SearchText = string.Empty;
        _ = LoadCredentialsAsync(value?.Id);
    }

    partial void OnSelectedCredentialChanged(CredentialListItemResponse? value)
    {
        ResetRevealedValues();
        CancelEditState();
    }

    partial void OnSearchTextChanged(string? value)
    {
        ApplyCredentialFilter(preserveSelection: true);
    }

    private async Task LoadCredentialsAsync(long? companyId)
    {
        IsLoading = true;

        try
        {
            ResetRevealedValues();

            var list = await _credentialsService.GetAllAsync(companyId);

            _allCredentials.Clear();

            foreach (var credential in list.OrderBy(c => c.Title))
            {
                _allCredentials.Add(credential);
            }

            ApplyCredentialFilter(preserveSelection: false);
        }
        catch (ApiException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                _toastService.ShowWarning("A munkamenet lejárt — jelentkezz be újra.");
            }
            else
            {
                _toastService.ShowError($"Bejegyzések betöltése: {ex.Message}");
            }

            _logger.LogError(ex, "Credentials load error: {Status}", ex.StatusCode);
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Hálózati hiba: {ex.Message}");
            _logger.LogError(ex, "Credentials load exception.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyCredentialFilter(bool preserveSelection)
    {
        var selectedCredentialId = preserveSelection
            ? SelectedCredential?.Id
            : null;

        var filteredCredentials = GetFilteredCredentials();

        Credentials.Clear();

        foreach (var credential in filteredCredentials)
        {
            Credentials.Add(credential);
        }

        if (selectedCredentialId is not null)
        {
            SelectedCredential = Credentials.FirstOrDefault(x => x.Id == selectedCredentialId.Value);
        }

        if (SelectedCredential is null || !Credentials.Any(x => x.Id == SelectedCredential.Id))
        {
            SelectedCredential = Credentials.FirstOrDefault();
        }
    }

    private IEnumerable<CredentialListItemResponse> GetFilteredCredentials()
    {
        var query = SearchText?.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            return _allCredentials.OrderBy(c => c.Title);
        }

        return _allCredentials
            .Where(c => CredentialMatchesSearch(c, query))
            .OrderBy(c => c.Title);
    }

    private static bool CredentialMatchesSearch(CredentialListItemResponse credential, string query)
    {
        return ContainsSearchText(credential.Title, query) ||
               ContainsSearchText(credential.Notes, query) ||
               ContainsSearchText(credential.ConnectionValue, query);
    }

    private static bool ContainsSearchText(string? value, string query)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private bool CanStartEdit()
    {
        return !IsLoading &&
               !IsEditMode &&
               CanEditDeleteCredentials &&
               SelectedCredential is not null;
    }

    [RelayCommand(CanExecute = nameof(CanStartEdit))]
    private async Task StartEditAsync()
    {
        if (SelectedCredential is null)
        {
            return;
        }

        IsLoading = true;
        EditErrorMessage = null;

        try
        {
            var usernameResponse = await _credentialsService.RevealUsernameAsync(SelectedCredential.Id);
            var passwordResponse = await _credentialsService.RevealPasswordAsync(SelectedCredential.Id);

            EditTitle = SelectedCredential.Title;
            EditUsername = usernameResponse?.Username ?? string.Empty;
            EditPassword = passwordResponse?.Password ?? string.Empty;
            EditConnectionValue = SelectedCredential.ConnectionValue;
            EditNotes = SelectedCredential.Notes;

            IsEditMode = true;
        }
        catch (ApiException ex)
        {
            EditErrorMessage = ex.Message;
            _toastService.ShowError($"Szerkesztés indítása sikertelen: {ex.Message}");
            _logger.LogWarning(ex, "Start edit failed.");
        }
        catch (Exception ex)
        {
            EditErrorMessage = ex.Message;
            _toastService.ShowError($"Váratlan hiba: {ex.Message}");
            _logger.LogError(ex, "Start edit unexpected error.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanSaveEdit()
    {
        return !IsLoading &&
               IsEditMode &&
               SelectedCredential is not null &&
               !string.IsNullOrWhiteSpace(EditTitle) &&
               !string.IsNullOrWhiteSpace(EditUsername) &&
               !string.IsNullOrWhiteSpace(EditPassword);
    }

    [RelayCommand(CanExecute = nameof(CanSaveEdit))]
    private async Task SaveEditAsync()
    {
        if (SelectedCredential is null)
        {
            return;
        }

        IsLoading = true;
        EditErrorMessage = null;

        var editedCredentialId = SelectedCredential.Id;
        var currentCompanyId = SelectedCompany?.Id;

        try
        {
            var request = new UpdateCredentialRequest
            {
                Title = EditTitle.Trim(),
                Username = EditUsername.Trim(),
                Password = EditPassword,
                ConnectionValue = string.IsNullOrWhiteSpace(EditConnectionValue)
                    ? null
                    : EditConnectionValue.Trim(),
                Notes = string.IsNullOrWhiteSpace(EditNotes)
                    ? null
                    : EditNotes.Trim(),
                IsActive = true
            };

            await _credentialsService.UpdateAsync(editedCredentialId, request);

            IsEditMode = false;
            ResetRevealedValues();

            await LoadCredentialsAsync(currentCompanyId);

            SelectedCredential = Credentials.FirstOrDefault(x => x.Id == editedCredentialId);

            _toastService.ShowSuccess("Bejegyzés módosítva.");
        }
        catch (ApiException ex)
        {
            EditErrorMessage = ex.Message;
            _toastService.ShowError($"Mentés sikertelen: {ex.Message}");
            _logger.LogWarning(ex, "Save edit failed.");
        }
        catch (Exception ex)
        {
            EditErrorMessage = ex.Message;
            _toastService.ShowError($"Váratlan hiba: {ex.Message}");
            _logger.LogError(ex, "Save edit unexpected error.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanCancelEdit()
    {
        return IsEditMode;
    }

    [RelayCommand(CanExecute = nameof(CanCancelEdit))]
    private void CancelEdit()
    {
        CancelEditState();
    }

    private void CancelEditState()
    {
        IsEditMode = false;
        EditErrorMessage = null;
        EditTitle = string.Empty;
        EditUsername = string.Empty;
        EditPassword = string.Empty;
        EditConnectionValue = null;
        EditNotes = null;
    }

    private bool CanDeleteCredential()
    {
        return !IsLoading &&
               !IsEditMode &&
               CanEditDeleteCredentials &&
               SelectedCredential is not null;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteCredential))]
    private async Task DeleteCredentialAsync()
    {
        if (SelectedCredential is null)
        {
            return;
        }

        var credentialId = SelectedCredential.Id;
        var currentCompanyId = SelectedCompany?.Id;

        var owner = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(x => x.IsActive)
            ?? Application.Current.MainWindow;

        var confirmWindow = new DeleteConfirmWindow
        {
            Owner = owner
        };

        var result = confirmWindow.ShowDialog();

        if (result != true || !confirmWindow.IsConfirmed)
        {
            return;
        }

        IsLoading = true;

        try
        {
            await _credentialsService.DeleteAsync(credentialId);

            ResetRevealedValues();
            CancelEditState();

            await LoadCredentialsAsync(currentCompanyId);

            _toastService.ShowSuccess("Bejegyzés törölve.");
        }
        catch (ApiException ex)
        {
            _toastService.ShowError($"Törlés sikertelen: {ex.Message}");
            _logger.LogWarning(ex, "Delete credential failed.");
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Váratlan hiba: {ex.Message}");
            _logger.LogError(ex, "Delete credential unexpected error.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ResetRevealedValues()
    {
        _autoMaskCts?.Cancel();

        RevealedUsername = null;
        RevealedPassword = null;
        IsUsernameRevealed = false;
        IsPasswordRevealed = false;
    }

    private bool HasSelected()
    {
        return SelectedCredential is not null;
    }

    [RelayCommand(CanExecute = nameof(HasSelected))]
    private async Task RevealUsernameAsync()
    {
        if (SelectedCredential is null)
        {
            return;
        }

        try
        {
            var response = await _credentialsService.RevealUsernameAsync(SelectedCredential.Id);

            if (response is null)
            {
                return;
            }

            RevealedUsername = response.Username;
            IsUsernameRevealed = true;

            ScheduleAutoMask();
        }
        catch (ApiException ex)
        {
            _toastService.ShowError($"Felhasználónév feltárás: {ex.Message}");
            _logger.LogWarning(ex, "RevealUsername failed.");
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelected))]
    private async Task RevealPasswordAsync()
    {
        if (SelectedCredential is null)
        {
            return;
        }

        try
        {
            var response = await _credentialsService.RevealPasswordAsync(SelectedCredential.Id);

            if (response is null)
            {
                return;
            }

            RevealedPassword = response.Password;
            IsPasswordRevealed = true;

            ScheduleAutoMask();
        }
        catch (ApiException ex)
        {
            _toastService.ShowError($"Jelszó feltárás: {ex.Message}");
            _logger.LogWarning(ex, "RevealPassword failed.");
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelected))]
    private async Task CopyUsernameAsync()
    {
        if (SelectedCredential is null)
        {
            return;
        }

        try
        {
            var username = RevealedUsername;

            if (string.IsNullOrEmpty(username))
            {
                var response = await _credentialsService.RevealUsernameAsync(SelectedCredential.Id);
                username = response?.Username;
            }

            if (!string.IsNullOrEmpty(username))
            {
                _clipboardService.CopyWithAutoClear(username);
                _toastService.ShowSuccess(
                    $"Felhasználónév vágólapra másolva (törlés {_sessionSettings.ClipboardClearSeconds} mp múlva)"
                );
            }
        }
        catch (ApiException ex)
        {
            _toastService.ShowError($"Másolás sikertelen: {ex.Message}");
            _logger.LogWarning(ex, "CopyUsername failed.");
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelected))]
    private async Task CopyPasswordAsync()
    {
        if (SelectedCredential is null)
        {
            return;
        }

        try
        {
            var password = RevealedPassword;

            if (string.IsNullOrEmpty(password))
            {
                var response = await _credentialsService.RevealPasswordAsync(SelectedCredential.Id);
                password = response?.Password;
            }

            if (!string.IsNullOrEmpty(password))
            {
                _clipboardService.CopyWithAutoClear(password);
                _toastService.ShowSuccess(
                    $"Jelszó vágólapra másolva (törlés {_sessionSettings.ClipboardClearSeconds} mp múlva)"
                );
            }
        }
        catch (ApiException ex)
        {
            _toastService.ShowError($"Másolás sikertelen: {ex.Message}");
            _logger.LogWarning(ex, "CopyPassword failed.");
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelected))]
    private void OpenConnection()
    {
        var connection = SelectedCredential?.ConnectionValue;

        if (string.IsNullOrWhiteSpace(connection))
        {
            _toastService.ShowInfo("Ehhez a bejegyzéshez nincs kapcsolódási információ.");
            return;
        }

        try
        {
            if (connection.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                connection.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                Process.Start(new ProcessStartInfo(connection)
                {
                    UseShellExecute = true
                });

                return;
            }

            if (connection.StartsWith("\\\\") ||
                connection.EndsWith(".rdp", StringComparison.OrdinalIgnoreCase))
            {
                Process.Start(new ProcessStartInfo(connection)
                {
                    UseShellExecute = true
                });

                return;
            }

            Process.Start(new ProcessStartInfo(connection)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Megnyitás sikertelen: {ex.Message}");
            _logger.LogWarning(ex, "OpenConnection failed for {Value}", connection);
        }
    }

    [RelayCommand]
    private void ToggleHidePassword()
    {
        IsPasswordRevealed = false;
        RevealedPassword = null;
    }

    [RelayCommand]
    private void ToggleHideUsername()
    {
        IsUsernameRevealed = false;
        RevealedUsername = null;
    }

    private void ScheduleAutoMask()
    {
        _autoMaskCts?.Cancel();

        _autoMaskCts = new CancellationTokenSource();

        var token = _autoMaskCts.Token;
        var seconds = Math.Max(1, _sessionSettings.PasswordRevealAutoMaskSeconds);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds), token);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsPasswordRevealed = false;
                    IsUsernameRevealed = false;
                    RevealedUsername = null;
                    RevealedPassword = null;
                });
            }
            catch (TaskCanceledException)
            {
            }
        }, token);
    }
}