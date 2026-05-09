using System.Windows;
using System.Windows.Controls;

namespace PasswordManagerSystem.Client.Behaviors;

/// <summary>
/// PasswordBox.Password alapból nem bindolható biztonsági okból.
/// Ezzel az attached property-vel mégis kétirányúan bindolhatóvá tesszük,
/// úgy hogy a Password property nem hagyja el a UI réteget memóriában.
///
/// Használat XAML-ben:
///   <PasswordBox b:PasswordBoxBindingBehavior.Password="{Binding Password, Mode=TwoWay}"/>
/// </summary>
public static class PasswordBoxBindingBehavior
{
    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.RegisterAttached(
            "Password",
            typeof(string),
            typeof(PasswordBoxBindingBehavior),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnPasswordPropertyChanged));

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(PasswordBoxBindingBehavior));

    public static string GetPassword(DependencyObject d)
        => (string)d.GetValue(PasswordProperty);

    public static void SetPassword(DependencyObject d, string value)
        => d.SetValue(PasswordProperty, value);

    private static bool GetIsUpdating(DependencyObject d)
        => (bool)d.GetValue(IsUpdatingProperty);

    private static void SetIsUpdating(DependencyObject d, bool value)
        => d.SetValue(IsUpdatingProperty, value);

    private static void OnPasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox passwordBox)
        {
            return;
        }

        passwordBox.PasswordChanged -= HandlePasswordChanged;

        if (!GetIsUpdating(passwordBox))
        {
            passwordBox.Password = (string)(e.NewValue ?? string.Empty);
        }

        passwordBox.PasswordChanged += HandlePasswordChanged;
    }

    private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
        {
            return;
        }

        SetIsUpdating(passwordBox, true);
        SetPassword(passwordBox, passwordBox.Password);
        SetIsUpdating(passwordBox, false);
    }
}
