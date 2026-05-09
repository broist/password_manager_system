using System.Net;
using System.Net.Http;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PasswordManagerSystem.Client.Services.Api;
using PasswordManagerSystem.Client.Services.Auth;
using PasswordManagerSystem.Client.Services.Session;

namespace PasswordManagerSystem.Client.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<LoginViewModel> _logger;

    public LoginViewModel(
        IAuthenticationService authenticationService,
        ISessionService sessionService,
        ILogger<LoginViewModel> logger)
    {
        _authenticationService = authenticationService;
        _sessionService = sessionService;
        _logger = logger;
    }

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _loginSucceeded;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Add meg a felhasználónevet.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Add meg a jelszót.";
            return;
        }

        ErrorMessage = null;
        IsBusy = true;

        try
        {
            var loginResponse = await _authenticationService
                .LoginAsync(Username.Trim(), Password, CancellationToken.None)
                .ConfigureAwait(true);

            _sessionService.SetSession(loginResponse);

            _logger.LogInformation(
                "Sikeres bejelentkezés: {User} (szerepkör: {Role})",
                loginResponse.User.AdUsername,
                loginResponse.Role.Name);

            LoginSucceeded = true;
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = apiEx.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Hibás felhasználónév vagy jelszó.",
                HttpStatusCode.Forbidden => "Nincs jogosultságod a rendszerhez.",
                _ => apiEx.Message
            };
            _logger.LogWarning(apiEx, "Login API hiba: {Status}", apiEx.StatusCode);
        }
        catch (HttpRequestException httpEx)
        {
            ErrorMessage = "Nem sikerült csatlakozni a backendhez. " +
                          "Ellenőrizd, hogy fut-e az API, és helyes-e a BaseUrl az appsettings.json-ben.";
            _logger.LogError(httpEx, "Login hálózati hiba.");
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "A bejelentkezés időtúllépést kapott. Próbáld újra.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Váratlan hiba: {ex.Message}";
            _logger.LogError(ex, "Login váratlan hiba.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Exit()
    {
        Application.Current.Shutdown();
    }
}