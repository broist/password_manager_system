using System.Windows;
using System.Windows.Input;

namespace PasswordManagerSystem.Client.Views.Credentials;

public partial class DeleteConfirmWindow : Window
{
    private const string RequiredConfirmationText = "permanently delete";

    public DeleteConfirmWindow()
    {
        InitializeComponent();
    }

    public bool IsConfirmed { get; private set; }

    private void CopyRequiredTextButton_OnClick(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(RequiredConfirmationText);
        ConfirmInput.Focus();
    }

    private void DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;

        var input = ConfirmInput.Text?.Trim();

        if (!string.Equals(input, RequiredConfirmationText, StringComparison.Ordinal))
        {
            ErrorText.Text = "A megadott szoveg nem egyezik. Torles nem tortent.";
            IsConfirmed = false;
            return;
        }

        IsConfirmed = true;
        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        DialogResult = false;
        Close();
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
