using System.Windows;
using System.Windows.Input;

namespace PasswordManagerSystem.Client.Views.Companies;

public partial class DeactivateCompanyConfirmWindow : Window
{
    public DeactivateCompanyConfirmWindow(string companyName)
    {
        InitializeComponent();
        CompanyNameText.Text = companyName;
    }

    public bool IsConfirmed { get; private set; }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
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