using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using PasswordManagerSystem.Client.ViewModels;

namespace PasswordManagerSystem.Client.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        Loaded += (_, _) =>
        {
            UsernameTextBox.Focus();
        };

        // Password mező kötése code-behind-ból (PasswordBox biztonsági okból
        // nem tud Password property-re bindolni közvetlenül)
        PasswordBox.PasswordChanged += (_, _) =>
        {
            _viewModel.Password = PasswordBox.Password;
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LoginViewModel.LoginSucceeded) && _viewModel.LoginSucceeded)
        {
            var shell = App.Services.GetRequiredService<ShellWindow>();
            Application.Current.MainWindow = shell;
            shell.Show();

            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            Close();
        }
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.LoginCommand.Execute(null);
            e.Handled = true;
        }
    }
}