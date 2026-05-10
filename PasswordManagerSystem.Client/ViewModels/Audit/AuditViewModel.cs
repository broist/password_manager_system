using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PasswordManagerSystem.Client.Models.Audit;
using PasswordManagerSystem.Client.Services.Api;
using PasswordManagerSystem.Client.Services.Notifications;

namespace PasswordManagerSystem.Client.ViewModels.Audit;

public sealed partial class AuditViewModel : ObservableObject
{
    private readonly IAuditApiService _auditApiService;
    private readonly IToastService _toastService;
    private readonly ILogger<AuditViewModel> _logger;

    public AuditViewModel(
        IAuditApiService auditApiService,
        IToastService toastService,
        ILogger<AuditViewModel> logger)
    {
        _auditApiService = auditApiService;
        _toastService = toastService;
        _logger = logger;

        _ = InitializeAsync();
    }

    public ObservableCollection<AuditLogResponse> AuditLogs { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(VerifyChainCommand))]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _actionFilter;

    [ObservableProperty]
    private string? _adUsernameFilter;

    [ObservableProperty]
    private bool? _successFilter;

    [ObservableProperty]
    private int _take = 200;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _returnedCount;

    [ObservableProperty]
    private AuditChainVerificationResponse? _chainVerification;

    public bool HasAuditLogs => AuditLogs.Count > 0;

    public string ChainStatusText
    {
        get
        {
            if (ChainVerification is null)
            {
                return "Audit lánc még nincs ellenőrizve.";
            }

            return ChainVerification.IsValid
                ? "Audit hash-chain érvényes."
                : "Audit hash-chain sérült.";
        }
    }

    public string ChainStatusDetails
    {
        get
        {
            if (ChainVerification is null)
            {
                return "Indíts ellenőrzést az integritási állapot megjelenítéséhez.";
            }

            if (ChainVerification.IsValid)
            {
                return $"{ChainVerification.CheckedRecords} audit rekord ellenőrizve. {ChainVerification.Message}";
            }

            return $"Sérülés helye: audit_log.id={ChainVerification.BrokenAtAuditLogId}. {ChainVerification.Message}";
        }
    }

    public bool IsChainValid => ChainVerification?.IsValid == true;

    public bool IsChainBroken => ChainVerification is not null && !ChainVerification.IsValid;

    partial void OnChainVerificationChanged(AuditChainVerificationResponse? value)
    {
        OnPropertyChanged(nameof(ChainStatusText));
        OnPropertyChanged(nameof(ChainStatusDetails));
        OnPropertyChanged(nameof(IsChainValid));
        OnPropertyChanged(nameof(IsChainBroken));
    }

    private async Task InitializeAsync()
    {
        await LoadAuditLogsAsync();
    }

    private bool CanExecuteApiCommand()
    {
        return !IsLoading;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteApiCommand))]
    private async Task RefreshAsync()
    {
        await LoadAuditLogsAsync();
    }

    [RelayCommand(CanExecute = nameof(CanExecuteApiCommand))]
    private async Task VerifyChainAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            ChainVerification = await _auditApiService.VerifyChainAsync();

            if (ChainVerification is null)
            {
                _toastService.ShowWarning("Az audit lánc ellenőrzése nem adott vissza eredményt.");
                return;
            }

            if (ChainVerification.IsValid)
            {
                _toastService.ShowSuccess("Audit hash-chain érvényes.");
            }
            else
            {
                _toastService.ShowError("Audit hash-chain sérült.");
            }
        }
        catch (ApiException ex)
        {
            HandleApiException(ex, "Audit hash-chain ellenőrzése sikertelen.");
            _logger.LogWarning(ex, "Audit chain verification failed.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toastService.ShowError($"Váratlan hiba: {ex.Message}");
            _logger.LogError(ex, "Audit chain verification unexpected error.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        ActionFilter = null;
        AdUsernameFilter = null;
        SuccessFilter = null;
        Take = 200;

        _ = LoadAuditLogsAsync();
    }

    [RelayCommand]
    private void SetSuccessFilter()
    {
        SuccessFilter = true;
        _ = LoadAuditLogsAsync();
    }

    [RelayCommand]
    private void SetFailedFilter()
    {
        SuccessFilter = false;
        _ = LoadAuditLogsAsync();
    }

    [RelayCommand]
    private void ClearSuccessFilter()
    {
        SuccessFilter = null;
        _ = LoadAuditLogsAsync();
    }

    private async Task LoadAuditLogsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var response = await _auditApiService.GetLogsAsync(
                take: Take,
                action: ActionFilter,
                adUsername: AdUsernameFilter,
                success: SuccessFilter);

            AuditLogs.Clear();

            if (response is null)
            {
                TotalCount = 0;
                ReturnedCount = 0;
                OnPropertyChanged(nameof(HasAuditLogs));
                return;
            }

            foreach (var item in response.Items.OrderByDescending(x => x.Id))
            {
                AuditLogs.Add(item);
            }

            TotalCount = response.TotalCount;
            ReturnedCount = response.ReturnedCount;

            OnPropertyChanged(nameof(HasAuditLogs));
        }
        catch (ApiException ex)
        {
            HandleApiException(ex, "Audit napló betöltése sikertelen.");
            _logger.LogWarning(ex, "Audit log load failed.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toastService.ShowError($"Váratlan hiba: {ex.Message}");
            _logger.LogError(ex, "Audit log load unexpected error.");
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
            _toastService.ShowWarning($"{messagePrefix} Csak ITAdmin jogosultsággal érhető el.");
            return;
        }

        _toastService.ShowError($"{messagePrefix} {ex.Message}");
    }
}