using System.Windows;
using System.Windows.Input;

namespace PasswordManagerSystem.Client.Views.Credentials;

public partial class CredentialEditorWindow : Window
{
    public CredentialEditorWindow(
        PasswordManagerSystem.Client.ViewModels.Credentials.CredentialEditorViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        viewModel.CloseRequested += OnCloseRequested;
        viewModel.PasswordGenerated += OnPasswordGenerated;
    }

    private void PasswordInput_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is PasswordManagerSystem.Client.ViewModels.Credentials.CredentialEditorViewModel viewModel)
        {
            viewModel.Password = PasswordInput.Password;
        }
    }

    private async void GenerateButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not PasswordManagerSystem.Client.ViewModels.Credentials.CredentialEditorViewModel viewModel)
        {
            return;
        }

        var generatorWindow = new PasswordGeneratorOptionsWindow
        {
            Owner = this
        };

        var result = generatorWindow.ShowDialog();

        if (result != true || generatorWindow.Result is null)
        {
            return;
        }

        await viewModel.GeneratePasswordWithOptionsAsync(
            generatorWindow.Result.Length,
            generatorWindow.Result.IncludeUppercase,
            generatorWindow.Result.IncludeLowercase,
            generatorWindow.Result.IncludeDigits,
            generatorWindow.Result.IncludeSpecialCharacters
        );
    }

    private void OnPasswordGenerated(object? sender, string password)
    {
        PasswordInput.Password = password;
    }

    private void OnCloseRequested(object? sender, bool result)
    {
        DialogResult = result;
        Close();
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is PasswordManagerSystem.Client.ViewModels.Credentials.CredentialEditorViewModel viewModel)
        {
            viewModel.CloseRequested -= OnCloseRequested;
            viewModel.PasswordGenerated -= OnPasswordGenerated;
        }

        base.OnClosed(e);
    }
}