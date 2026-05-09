using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PasswordManagerSystem.Client.Models.Companies;
using PasswordManagerSystem.Client.Services.Api;
using PasswordManagerSystem.Client.Services.Notifications;

namespace PasswordManagerSystem.Client.ViewModels.Companies;

public sealed partial class CompaniesViewModel : ObservableObject
{
    private readonly ICompaniesService _companiesService;
    private readonly IToastService _toastService;
    private readonly ILogger<CompaniesViewModel> _logger;

    public CompaniesViewModel(
        ICompaniesService companiesService,
        IToastService toastService,
        ILogger<CompaniesViewModel> logger)
    {
        _companiesService = companiesService;
        _toastService = toastService;
        _logger = logger;

        _ = LoadCompaniesAsync();
    }

    public ObservableCollection<CompanyResponse> Companies { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedCompany))]
    [NotifyCanExecuteChangedFor(nameof(StartEditCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveEditCommand))]
    private CompanyResponse? _selectedCompany;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCreateCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartEditCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCreateCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveEditCommand))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReadOnlyMode))]
    [NotifyCanExecuteChangedFor(nameof(StartCreateCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartEditCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCreateCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCreateCommand))]
    private bool _isCreateMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReadOnlyMode))]
    [NotifyCanExecuteChangedFor(nameof(StartCreateCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartEditCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveEditCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelEditCommand))]
    private bool _isEditMode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCreateCommand))]
    private string _newCompanyName = string.Empty;

    [ObservableProperty]
    private string? _newCompanyDescription;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveEditCommand))]
    private string _editCompanyName = string.Empty;

    [ObservableProperty]
    private string? _editCompanyDescription;

    [ObservableProperty]
    private string? _errorMessage;

    public bool HasSelectedCompany => SelectedCompany is not null;

    public bool IsReadOnlyMode => !IsCreateMode && !IsEditMode;

    [RelayCommand]
    private async Task LoadCompaniesAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var selectedCompanyId = SelectedCompany?.Id;

            var companies = await _companiesService.GetAllAsync();

            Companies.Clear();

            foreach (var company in companies.OrderBy(c => c.Name))
            {
                Companies.Add(company);
            }

            if (selectedCompanyId is not null)
            {
                SelectedCompany = Companies.FirstOrDefault(x => x.Id == selectedCompanyId.Value);
            }

            SelectedCompany ??= Companies.FirstOrDefault();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            _toastService.ShowError($"Cégek betöltése sikertelen: {ex.Message}");
            _logger.LogWarning(ex, "Companies load failed.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toastService.ShowError($"Váratlan hiba: {ex.Message}");
            _logger.LogError(ex, "Companies load unexpected error.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanStartCreate()
    {
        return !IsLoading && !IsCreateMode && !IsEditMode;
    }

    [RelayCommand(CanExecute = nameof(CanStartCreate))]
    private void StartCreate()
    {
        ErrorMessage = null;
        SelectedCompany = null;
        NewCompanyName = string.Empty;
        NewCompanyDescription = null;
        IsEditMode = false;
        IsCreateMode = true;
    }

    private bool CanCancelCreate()
    {
        return IsCreateMode;
    }

    [RelayCommand(CanExecute = nameof(CanCancelCreate))]
    private void CancelCreate()
    {
        ErrorMessage = null;
        NewCompanyName = string.Empty;
        NewCompanyDescription = null;
        IsCreateMode = false;

        SelectedCompany ??= Companies.FirstOrDefault();
    }

    private bool CanSaveCreate()
    {
        return !IsLoading &&
               IsCreateMode &&
               !string.IsNullOrWhiteSpace(NewCompanyName);
    }

    [RelayCommand(CanExecute = nameof(CanSaveCreate))]
    private async Task SaveCreateAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var request = new CreateCompanyRequest
            {
                Name = NewCompanyName.Trim(),
                Description = string.IsNullOrWhiteSpace(NewCompanyDescription)
                    ? null
                    : NewCompanyDescription.Trim()
            };

            var createdCompany = await _companiesService.CreateAsync(request);

            NewCompanyName = string.Empty;
            NewCompanyDescription = null;
            IsCreateMode = false;

            await LoadCompaniesAsync();

            if (createdCompany is not null)
            {
                SelectedCompany = Companies.FirstOrDefault(x => x.Id == createdCompany.Id);
            }

            _toastService.ShowSuccess("Cég létrehozva.");
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            _toastService.ShowError($"Cég létrehozása sikertelen: {ex.Message}");
            _logger.LogWarning(ex, "Company create failed.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toastService.ShowError($"Váratlan hiba: {ex.Message}");
            _logger.LogError(ex, "Company create unexpected error.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanStartEdit()
    {
        return !IsLoading &&
               !IsCreateMode &&
               !IsEditMode &&
               SelectedCompany is not null;
    }

    [RelayCommand(CanExecute = nameof(CanStartEdit))]
    private void StartEdit()
    {
        if (SelectedCompany is null)
        {
            return;
        }

        ErrorMessage = null;
        EditCompanyName = SelectedCompany.Name;
        EditCompanyDescription = SelectedCompany.Description;
        IsCreateMode = false;
        IsEditMode = true;
    }

    private bool CanCancelEdit()
    {
        return IsEditMode;
    }

    [RelayCommand(CanExecute = nameof(CanCancelEdit))]
    private void CancelEdit()
    {
        ErrorMessage = null;
        EditCompanyName = string.Empty;
        EditCompanyDescription = null;
        IsEditMode = false;
    }

    private bool CanSaveEdit()
    {
        return !IsLoading &&
               IsEditMode &&
               SelectedCompany is not null &&
               !string.IsNullOrWhiteSpace(EditCompanyName);
    }

    [RelayCommand(CanExecute = nameof(CanSaveEdit))]
    private async Task SaveEditAsync()
    {
        if (SelectedCompany is null)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        var editedCompanyId = SelectedCompany.Id;

        try
        {
            var request = new UpdateCompanyRequest
            {
                Name = EditCompanyName.Trim(),
                Description = string.IsNullOrWhiteSpace(EditCompanyDescription)
                    ? null
                    : EditCompanyDescription.Trim(),
                IsActive = true
            };

            await _companiesService.UpdateAsync(editedCompanyId, request);

            EditCompanyName = string.Empty;
            EditCompanyDescription = null;
            IsEditMode = false;

            await LoadCompaniesAsync();

            SelectedCompany = Companies.FirstOrDefault(x => x.Id == editedCompanyId);

            _toastService.ShowSuccess("Cég módosítva.");
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            _toastService.ShowError($"Cég módosítása sikertelen: {ex.Message}");
            _logger.LogWarning(ex, "Company update failed.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toastService.ShowError($"Váratlan hiba: {ex.Message}");
            _logger.LogError(ex, "Company update unexpected error.");
        }
        finally
        {
            IsLoading = false;
        }
    }
}