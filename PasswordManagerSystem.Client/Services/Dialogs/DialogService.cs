using System.Windows;

namespace PasswordManagerSystem.Client.Services.Dialogs;

public enum MessageBoxResult
{
    Ok,
    Cancel,
    Yes,
    No
}

public enum MessageBoxKind
{
    Info,
    Warning,
    Error,
    Question
}

/// <summary>
/// Modal dialógus szolgáltatás. A ViewModel rétegnek nem kell tudnia
/// semmit a Window osztályról — itt absztraháljuk.
/// </summary>
public interface IDialogService
{
    Task<MessageBoxResult> ShowMessageAsync(
        string title,
        string message,
        MessageBoxKind kind = MessageBoxKind.Info,
        bool yesNo = false);
}

public sealed class DialogService : IDialogService
{
    public Task<MessageBoxResult> ShowMessageAsync(
        string title,
        string message,
        MessageBoxKind kind = MessageBoxKind.Info,
        bool yesNo = false)
    {
        var image = kind switch
        {
            MessageBoxKind.Info => MessageBoxImage.Information,
            MessageBoxKind.Warning => MessageBoxImage.Warning,
            MessageBoxKind.Error => MessageBoxImage.Error,
            MessageBoxKind.Question => MessageBoxImage.Question,
            _ => MessageBoxImage.Information
        };

        var buttons = yesNo
            ? MessageBoxButton.YesNo
            : MessageBoxButton.OK;

        var result = Application.Current.Dispatcher.Invoke(() =>
            System.Windows.MessageBox.Show(message, title, buttons, image));

        var mapped = result switch
        {
            System.Windows.MessageBoxResult.OK => MessageBoxResult.Ok,
            System.Windows.MessageBoxResult.Yes => MessageBoxResult.Yes,
            System.Windows.MessageBoxResult.No => MessageBoxResult.No,
            _ => MessageBoxResult.Cancel
        };

        return Task.FromResult(mapped);
    }
}
