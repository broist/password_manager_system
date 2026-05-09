using System.Windows;
using PasswordManagerSystem.Client.ViewModels;

namespace PasswordManagerSystem.Client.Views;

public partial class ShellWindow : Window
{
    public ShellWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
