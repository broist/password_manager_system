using System.Windows;
using System.Windows.Input;

namespace PasswordManagerSystem.Client.Views.Credentials;

public partial class PasswordGeneratorOptionsWindow : Window
{
    private bool _isSyncingLength;

    public PasswordGeneratorOptionsWindow()
    {
        InitializeComponent();
    }

    public PasswordGeneratorOptionsResult? Result { get; private set; }

    private void LengthSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isSyncingLength || LengthInput is null)
        {
            return;
        }

        _isSyncingLength = true;
        LengthInput.Text = ((int)e.NewValue).ToString();
        _isSyncingLength = false;
    }

    private void LengthInput_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isSyncingLength || LengthSlider is null)
        {
            return;
        }

        if (!int.TryParse(LengthInput.Text, out var value))
        {
            return;
        }

        value = Math.Clamp(value, 8, 128);

        _isSyncingLength = true;
        LengthSlider.Value = value;
        _isSyncingLength = false;
    }

    private void GenerateButton_OnClick(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;

        if (!int.TryParse(LengthInput.Text, out var length))
        {
            ErrorText.Text = "A jelszo hossza ervenytelen.";
            return;
        }

        length = Math.Clamp(length, 8, 128);

        var includeUppercase = UppercaseCheck.IsChecked == true;
        var includeLowercase = LowercaseCheck.IsChecked == true;
        var includeDigits = DigitsCheck.IsChecked == true;
        var includeSpecialCharacters = SpecialCheck.IsChecked == true;

        if (!includeUppercase &&
            !includeLowercase &&
            !includeDigits &&
            !includeSpecialCharacters)
        {
            ErrorText.Text = "Legalabb egy karaktertipust ki kell valasztani.";
            return;
        }

        Result = new PasswordGeneratorOptionsResult(
            length,
            includeUppercase,
            includeLowercase,
            includeDigits,
            includeSpecialCharacters
        );

        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
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

public sealed class PasswordGeneratorOptionsResult
{
    public PasswordGeneratorOptionsResult(
        int length,
        bool includeUppercase,
        bool includeLowercase,
        bool includeDigits,
        bool includeSpecialCharacters)
    {
        Length = length;
        IncludeUppercase = includeUppercase;
        IncludeLowercase = includeLowercase;
        IncludeDigits = includeDigits;
        IncludeSpecialCharacters = includeSpecialCharacters;
    }

    public int Length { get; }

    public bool IncludeUppercase { get; }

    public bool IncludeLowercase { get; }

    public bool IncludeDigits { get; }

    public bool IncludeSpecialCharacters { get; }
}