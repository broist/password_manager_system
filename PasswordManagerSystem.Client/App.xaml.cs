using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PasswordManagerSystem.Client.Configuration;
using PasswordManagerSystem.Client.Services.Api;
using PasswordManagerSystem.Client.Services.Auth;
using PasswordManagerSystem.Client.Services.Clipboard;
using PasswordManagerSystem.Client.Services.Dialogs;
using PasswordManagerSystem.Client.Services.Notifications;
using PasswordManagerSystem.Client.Services.Session;
using PasswordManagerSystem.Client.ViewModels;
using PasswordManagerSystem.Client.ViewModels.Credentials;
using PasswordManagerSystem.Client.Views;
using PasswordManagerSystem.Client.ViewModels.Companies;
using PasswordManagerSystem.Client.ViewModels.Access;

namespace PasswordManagerSystem.Client;

/// <summary>
/// Az alkalmazás belépési pontja.
/// IHost-ot használunk DI / Configuration / Logging integrációhoz.
/// Biztonsági döntés: alkalmazásindításkor nincs automatikus beléptetés.
/// Minden új indításkor LoginWindow jelenik meg.
/// </summary>
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((ctx, config) =>
            {
                var basePath = AppContext.BaseDirectory;

                config.SetBasePath(basePath);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
            })
            .ConfigureServices(ConfigureServices)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug();

#if DEBUG
                logging.SetMinimumLevel(LogLevel.Debug);
#else
                logging.SetMinimumLevel(LogLevel.Information);
#endif
            })
            .Build();

        Services = _host.Services;

        await _host.StartAsync();

        var tokenStore = Services.GetRequiredService<ITokenStore>();

        // MVP biztonsági döntés:
        // ne induljon automatikus session visszatöltéssel a jelszókezelő.
        // Így minden alkalmazásindításkor új login szükséges.
        tokenStore.Clear();

        var loginWindow = Services.GetRequiredService<LoginWindow>();

        Current.MainWindow = loginWindow;
        loginWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // === Configuration ===
        services.Configure<AppSettings>(context.Configuration);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

        // === Auth core ===
        services.AddSingleton<ITokenStore, DpapiTokenStore>();

        // === HTTP handler AuthenticationService-hez ===
        services.AddSingleton<HttpMessageHandler>(sp =>
        {
            var settings = sp.GetRequiredService<AppSettings>();
            var handler = new HttpClientHandler();

            if (settings.Api.AcceptInvalidCertificates)
            {
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            return handler;
        });

        // === AuthenticationService saját HttpClienttel, AuthHeaderHandler nélkül ===
        services.AddSingleton<IAuthenticationService>(sp =>
        {
            var settings = sp.GetRequiredService<AppSettings>();
            var primaryHandler = sp.GetRequiredService<HttpMessageHandler>();

            var httpClient = new HttpClient(primaryHandler, disposeHandler: false)
            {
                BaseAddress = new Uri(settings.Api.BaseUrl),
                Timeout = settings.Api.Timeout
            };

            return new AuthenticationService(
                httpClient,
                sp.GetRequiredService<ITokenStore>(),
                sp.GetRequiredService<ILogger<AuthenticationService>>());
        });

        services.AddSingleton<ITokenRefresher>(sp =>
            sp.GetRequiredService<IAuthenticationService>());

        // === AuthHeaderHandler a fő API kliens elé ===
        services.AddTransient<AuthHeaderHandler>();

        // === Fő API kliens: Bearer token + 401 esetén refresh ===
        services.AddHttpClient<IApiClient, ApiClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<AppSettings>();

            client.BaseAddress = new Uri(settings.Api.BaseUrl);
            client.Timeout = settings.Api.Timeout;
        })
        .ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var settings = sp.GetRequiredService<AppSettings>();
            var handler = new HttpClientHandler();

            if (settings.Api.AcceptInvalidCertificates)
            {
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            return handler;
        })
        .AddHttpMessageHandler<AuthHeaderHandler>();

        // === Feature API service-ek ===
        services.AddSingleton<ICompaniesService, CompaniesService>();
        services.AddSingleton<ICredentialsService, CredentialsService>();
        services.AddSingleton<ICredentialAccessApiService, CredentialAccessApiService>();
		services.AddTransient<IUsersApiService, UsersApiService>();
        services.AddSingleton<IPasswordGeneratorService, PasswordGeneratorService>();
        services.AddSingleton<IAuditApiService, AuditApiService>();
        services.AddSingleton<IHealthService, HealthService>();
		

        // === Infrastruktúra ===
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<IClipboardService, SecureClipboardService>();
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton<IDialogService, DialogService>();

        // === ViewModel-ek ===
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ShellViewModel>();
        services.AddTransient<CredentialListViewModel>();
		services.AddTransient<CompaniesViewModel>();
		services.AddTransient<AccessViewModel>();

        services.AddTransient<Func<long, string, CredentialEditorViewModel>>(sp =>
        {
            return (companyId, companyName) =>
                new CredentialEditorViewModel(
                    sp.GetRequiredService<ICredentialsService>(),
                    sp.GetRequiredService<IPasswordGeneratorService>(),
                    sp.GetRequiredService<ICredentialAccessApiService>(),
                    companyId,
                    companyName
                );
        });

        // === Ablakok ===
        // Fontos: Window nem lehet Singleton, mert bezárás után nem nyitható újra.
        services.AddTransient<LoginWindow>();
        services.AddTransient<ShellWindow>();
    }
}