using System.Windows;
using Microsoft.Extensions.Options;
using PasswordManagerSystem.Client.Configuration;

namespace PasswordManagerSystem.Client.Services.Clipboard;

/// <summary>
/// Olyan vágólap-kezelő, ami a beállított idő után automatikusan törli azt,
/// amit kimásoltunk — feltéve hogy a vágólap tartalma azóta nem változott.
/// Profi PAM-ek így működnek: Bitwarden, KeePass, RDM mind.
/// </summary>
public interface IClipboardService
{
    /// <summary>Felmásol egy értéket, és időzít egy auto-clear műveletet.</summary>
    void CopyWithAutoClear(string value);
}

public sealed class SecureClipboardService : IClipboardService
{
    private readonly SessionSettings _settings;
    private CancellationTokenSource? _pendingClear;

    public SecureClipboardService(IOptions<AppSettings> options)
    {
        _settings = options.Value.Session;
    }

    public void CopyWithAutoClear(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        // Korábbi clear timer leállítása
        _pendingClear?.Cancel();
        _pendingClear = new CancellationTokenSource();
        var token = _pendingClear.Token;

        // Az aktuális UI thread-en kell vágólapot írni
        Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                System.Windows.Clipboard.SetDataObject(value, true);
            }
            catch
            {
                // Néha a vágólap foglalt lehet más app által — ilyenkor csendben lenyeljük
            }
        });

        var seconds = Math.Max(1, _settings.ClipboardClearSeconds);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds), token).ConfigureAwait(false);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // Csak akkor töröljük, ha még mindig a mi szövegünk van a vágólapon!
                        // Ha a user közben átmásolt valamit, hagyjuk békén.
                        if (System.Windows.Clipboard.ContainsText())
                        {
                            var current = System.Windows.Clipboard.GetText();
                            if (string.Equals(current, value, StringComparison.Ordinal))
                            {
                                System.Windows.Clipboard.Clear();
                            }
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                });
            }
            catch (TaskCanceledException)
            {
                // Egy újabb másolás érkezett — ez normális.
            }
        }, token);
    }
}
