using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PasswordManagerSystem.Client.Models.Common;
using PasswordManagerSystem.Client.Services.Auth;
using PasswordManagerSystem.Client.Services.Dialogs;
using PasswordManagerSystem.Client.Services.Notifications;
using PasswordManagerSystem.Client.Services.Session;
using PasswordManagerSystem.Client.ViewModels.Credentials;
using PasswordManagerSystem.Client.Views;

namespace PasswordManagerSystem.Client.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISessionService _sessionService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IDialogService _dialogService;
    private readonly IToastService _toastService;
    private readonly ILogger<ShellViewModel> _logger;

    public ShellViewModel(
        IServiceProvider serviceProvider,
        ISessionService sessionService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        IToastService toastService,
        ILogger<ShellViewModel> logger)
    {
        _serviceProvider = serviceProvider;
        _sessionService = sessionService;
        _authenticationService = authenticationService;
        _dialogService = dialogService;
        _toastService = toastService;
        _logger = logger;

        BuildNavigation();
        SelectedNavItem = NavigationItems.FirstOrDefault();
    }

    public IToastService Toasts => _toastService;

    public ObservableCollection<NavItem> NavigationItems { get; } = new();

    [ObservableProperty]
    private NavItem? _selectedNavItem;

    [ObservableProperty]
    private object? _currentView;

    public string UserDisplayName => _sessionService.CurrentUser?.DisplayName ?? "?";
    public string AdUsername => _sessionService.CurrentUser?.AdUsername ?? string.Empty;
    public string RoleDisplayName => _sessionService.CurrentRole?.DisplayName
                                     ?? _sessionService.CurrentRole?.Name
                                     ?? string.Empty;
    public string RoleName => _sessionService.CurrentRole?.Name ?? string.Empty;

    /// <summary>Az aktuális role-szín a badge-hez (amit a XAML triggers fog használni).</summary>
    public string RoleColorKey => _sessionService.CurrentRole?.Name switch
    {
        RoleNames.ItAdmin => "Brush.Role.ItAdmin",
        RoleNames.It => "Brush.Role.It",
        RoleNames.Consultant => "Brush.Role.Consultant",
        RoleNames.Support => "Brush.Role.Support",
        _ => "Brush.Text.Secondary"
    };

    private void BuildNavigation()
    {
        NavigationItems.Add(new NavItem("Bejegyzések", "Icon.Key", typeof(CredentialListViewModel)));
        NavigationItems.Add(new NavItem("Cégek", "Icon.Building", null) { IsPlaceholder = true });

        if (_sessionService.CanManageAccess)
        {
            NavigationItems.Add(new NavItem("Hozzáférések", "Icon.Shield", null) { IsPlaceholder = true });
            NavigationItems.Add(new NavItem("Audit napló", "Icon.Activity", null) { IsPlaceholder = true });
        }

        NavigationItems.Add(new NavItem("Beállítások", "Icon.Settings", null) { IsPlaceholder = true });
    }

    partial void OnSelectedNavItemChanged(NavItem? value)
    {
        if (value is null)
        {
            CurrentView = null;
            return;
        }

        if (value.IsPlaceholder)
        {
            CurrentView = new PlaceholderViewModel(value.Title);
            return;
        }

        if (value.ViewModelType is not null)
        {
            try
            {
                CurrentView = _serviceProvider.GetRequiredService(value.ViewModelType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nem sikerült betölteni a nézetet: {ViewModel}", value.ViewModelType.Name);
                _toastService.ShowError($"Hiba a nézet betöltésekor: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await _dialogService.ShowMessageAsync(
            "Kijelentkezés",
            "Biztosan ki szeretnél jelentkezni?",
            MessageBoxKind.Question,
            yesNo: true);

        if (confirm != Services.Dialogs.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _authenticationService.LogoutAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Logout API hiba — token mindenképpen törlődik.");
        }

        _sessionService.ClearSession();

        // Login ablak újranyitása, Shell bezárása
        Application.Current.Dispatcher.Invoke(() =>
        {
            var login = App.Services.GetRequiredService<LoginWindow>();
            Application.Current.MainWindow = login;
            login.Show();

            foreach (Window window in Application.Current.Windows)
            {
                if (window is ShellWindow shell)
                {
                    shell.Close();
                }
            }
        });
    }

    [RelayCommand]
    private void Minimize()
    {
        if (Application.Current.MainWindow is { } main)
        {
            main.WindowState = WindowState.Minimized;
        }
    }

    [RelayCommand]
    private void ToggleMaximize()
    {
        if (Application.Current.MainWindow is { } main)
        {
            main.WindowState = main.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
    }

    [RelayCommand]
    private void Close()
    {
        Application.Current.MainWindow?.Close();
    }
}

/// <summary>
/// Sidebar navigációs elem.
/// </summary>
public sealed class NavItem
{
    public NavItem(string title, string iconKey, Type? viewModelType)
    {
        Title = title;
        IconKey = iconKey;
        ViewModelType = viewModelType;
    }

    public string Title { get; }
    public string IconKey { get; }
    public Type? ViewModelType { get; }

    /// <summary>True ha placeholder — ilyenkor egy "hamarosan" típusú nézetet mutatunk.</summary>
    public bool IsPlaceholder { get; init; }
}

/// <summary>
/// Helykitöltő ViewModel: olyan nézetekhez amik még nincsenek implementálva.
/// </summary>
public sealed class PlaceholderViewModel
{
    public PlaceholderViewModel(string featureName)
    {
        FeatureName = featureName;
    }

    public string FeatureName { get; }
}
