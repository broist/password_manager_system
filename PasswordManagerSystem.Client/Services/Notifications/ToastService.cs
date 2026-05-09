using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PasswordManagerSystem.Client.Services.Notifications;

public enum ToastSeverity
{
    Info,
    Success,
    Warning,
    Error
}

public sealed partial class Toast : ObservableObject
{
    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private ToastSeverity _severity = ToastSeverity.Info;

    [ObservableProperty]
    private bool _isVisible;
}

/// <summary>
/// Egyszerű toast/snackbar service. Az ablak tetején vagy alján
/// rövid ideig megjelenít egy üzenetet.
/// </summary>
public interface IToastService
{
    Toast Current { get; }

    void ShowInfo(string message);
    void ShowSuccess(string message);
    void ShowWarning(string message);
    void ShowError(string message);
}

public sealed class ToastService : IToastService
{
    private CancellationTokenSource? _pending;

    public Toast Current { get; } = new();

    public void ShowInfo(string message) => Show(message, ToastSeverity.Info, 3);
    public void ShowSuccess(string message) => Show(message, ToastSeverity.Success, 3);
    public void ShowWarning(string message) => Show(message, ToastSeverity.Warning, 4);
    public void ShowError(string message) => Show(message, ToastSeverity.Error, 5);

    private void Show(string message, ToastSeverity severity, int seconds)
    {
        _pending?.Cancel();
        _pending = new CancellationTokenSource();
        var token = _pending.Token;

        Application.Current.Dispatcher.Invoke(() =>
        {
            Current.Message = message;
            Current.Severity = severity;
            Current.IsVisible = true;
        });

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds), token).ConfigureAwait(false);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Current.IsVisible = false;
                });
            }
            catch (TaskCanceledException)
            {
                // Új toast jött — ez normális.
            }
        }, token);
    }
}
