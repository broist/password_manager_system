using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PasswordManagerSystem.Client.Models.CredentialAccess;
using PasswordManagerSystem.Client.Models.Credentials;
using PasswordManagerSystem.Client.Models.PasswordGenerator;
using PasswordManagerSystem.Client.Services.Api;

namespace PasswordManagerSystem.Client.ViewModels.Credentials;

public sealed partial class CredentialEditorViewModel : ObservableObject
{
    private readonly ICredentialsService _credentialsService;
    private readonly IPasswordGeneratorService _passwordGeneratorService;
    private readonly ICredentialAccessApiService _credentialAccessApiService;

    public CredentialEditorViewModel(
        ICredentialsService credentialsService,
        IPasswordGeneratorService passwordGeneratorService,
        ICredentialAccessApiService credentialAccessApiService,
        long companyId,
        string companyName)
    {
        _credentialsService = credentialsService;
        _passwordGeneratorService = passwordGeneratorService;
        _credentialAccessApiService = credentialAccessApiService;

        CompanyId = companyId;
        CompanyName = companyName;

        VisibilityOptions.Add(new CredentialVisibilityOption("ITAdmin", new[] { 1L }));
        VisibilityOptions.Add(new CredentialVisibilityOption("IT", new[] { 1L, 2L }));
        VisibilityOptions.Add(new CredentialVisibilityOption("Consultant", new[] { 1L, 2L, 3L }));
        VisibilityOptions.Add(new CredentialVisibilityOption("Support", new[] { 1L, 2L, 3L, 4L }));

        SelectedVisibilityOption = VisibilityOptions.FirstOrDefault(x => x.Name == "IT");
    }

    public event EventHandler<bool>? CloseRequested;

    public event EventHandler<string>? PasswordGenerated;

    public long CompanyId { get; }

    public string CompanyName { get; }

    public ObservableCollection<CredentialVisibilityOption> VisibilityOptions { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private CredentialVisibilityOption? _selectedVisibilityOption;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _title = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _username = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _password = string.Empty;

    [ObservableProperty]
    private string? _connectionValue;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isGeneratingPassword;

    public CredentialDetailResponse? CreatedCredential { get; private set; }

    private bool CanSave()
    {
        return !IsSaving &&
               CompanyId > 0 &&
               SelectedVisibilityOption is not null &&
               !string.IsNullOrWhiteSpace(Title) &&
               !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password);
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        IsSaving = true;
        ErrorMessage = null;
        SaveCommand.NotifyCanExecuteChanged();

        try
        {
            var request = new CreateCredentialRequest
            {
                CompanyId = CompanyId,
                Title = Title.Trim(),
                Username = Username.Trim(),
                Password = Password,
                ConnectionValue = string.IsNullOrWhiteSpace(ConnectionValue)
                    ? null
                    : ConnectionValue.Trim(),
                Notes = string.IsNullOrWhiteSpace(Notes)
                    ? null
                    : Notes.Trim()
            };

            CreatedCredential = await _credentialsService.CreateAsync(request);

            if (CreatedCredential is null)
            {
                ErrorMessage = "A bejegyzes letrehozasa sikertelen.";
                return;
            }

            await ApplyRoleVisibilityAsync(CreatedCredential.Id);

            CloseRequested?.Invoke(this, true);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Varatlan hiba: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    public async Task GeneratePasswordWithOptionsAsync(
        int length,
        bool includeUppercase,
        bool includeLowercase,
        bool includeDigits,
        bool includeSpecialCharacters)
    {
        IsGeneratingPassword = true;
        ErrorMessage = null;

        try
        {
            var response = await _passwordGeneratorService.GenerateAsync(
                new GeneratePasswordRequest
                {
                    Length = length,
                    IncludeUppercase = includeUppercase,
                    IncludeLowercase = includeLowercase,
                    IncludeDigits = includeDigits,
                    IncludeSpecialCharacters = includeSpecialCharacters
                });

            if (response is null || string.IsNullOrWhiteSpace(response.Password))
            {
                ErrorMessage = "A jelszo generalasa sikertelen.";
                return;
            }

            Password = response.Password;
            PasswordGenerated?.Invoke(this, response.Password);
            SaveCommand.NotifyCanExecuteChanged();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Varatlan hiba: {ex.Message}";
        }
        finally
        {
            IsGeneratingPassword = false;
        }
    }

    private async Task ApplyRoleVisibilityAsync(long credentialId)
    {
        if (SelectedVisibilityOption is null)
        {
            return;
        }

        foreach (var roleId in SelectedVisibilityOption.VisibleRoleIds)
        {
            await _credentialAccessApiService.CreateAsync(
                new CreateCredentialAccessRequest
                {
                    CredentialId = credentialId,
                    RoleId = roleId,
                    UserId = null,
                    CanView = true,
                    CanWrite = roleId is 1 or 2,
                    CanDelete = roleId is 1 or 2,
                    ExpiresAt = null
                });
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, false);
    }
}

public sealed class CredentialVisibilityOption
{
    public CredentialVisibilityOption(string name, IReadOnlyList<long> visibleRoleIds)
    {
        Name = name;
        VisibleRoleIds = visibleRoleIds;
    }

    public string Name { get; }

    public IReadOnlyList<long> VisibleRoleIds { get; }
	
	public override string ToString()
	{
		return Name;
	}
}