using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PasswordManagerSystem.Client.Models.Users;

namespace PasswordManagerSystem.Client.Views.Access;

public partial class GrantTemporaryAccessWindow : Window
{
    private readonly List<ActiveUserResponse> _allUsers;
    private bool _isInternalTextChange;
    private ActiveUserResponse? _selectedUser;

    public GrantTemporaryAccessWindow(
        string credentialTitle,
        IReadOnlyList<ActiveUserResponse> users)
    {
        InitializeComponent();

        CredentialTitleText.Text = credentialTitle;

        _allUsers = users
            .OrderBy(x => x.AdUsername)
            .ToList();

        ApplyUserFilter(openDropdown: false);
		
		PreviewMouseDown += Window_OnPreviewMouseDown;

        Loaded += (_, _) =>
        {
            UserSearchTextBox.Focus();
            UserSearchTextBox.SelectAll();
        };
    }

    public bool IsConfirmed { get; private set; }

    public ActiveUserResponse? SelectedUser => _selectedUser;

    public int SelectedHours
    {
        get
        {
            var selectedRadio = FindVisualChildren<RadioButton>(this)
                .FirstOrDefault(x => x.IsChecked == true);

            if (selectedRadio?.Tag is string value &&
                int.TryParse(value, out var hours))
            {
                return hours;
            }

            return 1;
        }
    }

    private void UserSearchTextBox_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        ApplyUserFilter(openDropdown: true);
    }

    private void UserSearchTextBox_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!UserSearchTextBox.IsKeyboardFocusWithin)
        {
            e.Handled = true;
            UserSearchTextBox.Focus();
        }

        ApplyUserFilter(openDropdown: true);
    }

    private void UserSearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInternalTextChange)
        {
            return;
        }

        _selectedUser = null;
        ApplyUserFilter(openDropdown: UserSearchTextBox.IsKeyboardFocusWithin);
    }

    private void UserSearchTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down)
        {
            OpenDropdown();

            if (UserListBox.Items.Count > 0)
            {
                UserListBox.Focus();

                if (UserListBox.SelectedIndex < 0)
                {
                    UserListBox.SelectedIndex = 0;
                }

                var item = UserListBox.ItemContainerGenerator.ContainerFromIndex(UserListBox.SelectedIndex) as ListBoxItem;
                item?.Focus();
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            if (UserDropdownPopup.IsOpen && UserListBox.SelectedItem is ActiveUserResponse selectedFromList)
            {
                SelectUser(selectedFromList);
                e.Handled = true;
                return;
            }

            ConfirmSelection();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            CloseDropdown();
            e.Handled = true;
        }
    }

    private void UserListBox_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (UserListBox.SelectedItem is ActiveUserResponse selectedUser)
        {
            SelectUser(selectedUser);
        }
    }

    private void UserListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!UserListBox.IsKeyboardFocusWithin)
        {
            return;
        }

        if (Keyboard.IsKeyDown(Key.Enter) &&
            UserListBox.SelectedItem is ActiveUserResponse selectedUser)
        {
            SelectUser(selectedUser);
        }
    }

    private void UserDropdownPopup_OnClosed(object sender, EventArgs e)
    {
        UserSearchTextBox.SelectionLength = 0;
    }

    private void ApplyUserFilter(bool openDropdown)
    {
        HideValidationMessage();

        var query = UserSearchTextBox.Text?.Trim();

        var filteredUsers = string.IsNullOrWhiteSpace(query)
            ? _allUsers
            : _allUsers
                .Where(x =>
                    ContainsText(x.AdUsername, query) ||
                    ContainsText(x.DisplayName, query) ||
                    ContainsText(x.Email, query) ||
                    ContainsText(x.RoleName, query) ||
                    ContainsText(x.RoleDisplayName, query))
                .ToList();

        UserListBox.ItemsSource = filteredUsers;
        UserListBox.SelectedIndex = filteredUsers.Count > 0 ? 0 : -1;

        EmptyUsersTextBlock.Visibility = filteredUsers.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (openDropdown)
        {
            OpenDropdown();
        }
    }

    private void SelectUser(ActiveUserResponse user)
    {
        _selectedUser = user;

        _isInternalTextChange = true;
        UserSearchTextBox.Text = user.DisplayText;
        UserSearchTextBox.CaretIndex = UserSearchTextBox.Text.Length;
        _isInternalTextChange = false;

        CloseDropdown();
        UserSearchTextBox.Focus();
    }

    private void OpenDropdown()
    {
        if (!UserDropdownPopup.IsOpen)
        {
            UserDropdownPopup.IsOpen = true;
        }
    }

    private void CloseDropdown()
    {
        if (UserDropdownPopup.IsOpen)
        {
            UserDropdownPopup.IsOpen = false;
        }
    }

    private static bool ContainsText(string? value, string query)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        ConfirmSelection();
    }

    private void ConfirmSelection()
    {
        if (_selectedUser is null)
        {
            ShowValidationMessage("Valassz ki egy felhasznalot.");
            UserSearchTextBox.Focus();
            ApplyUserFilter(openDropdown: true);
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

    private void ShowValidationMessage(string message)
    {
        ValidationTextBlock.Text = message;
        ValidationTextBlock.Visibility = Visibility.Visible;
    }

    private void HideValidationMessage()
    {
        ValidationTextBlock.Text = string.Empty;
        ValidationTextBlock.Visibility = Visibility.Collapsed;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject dependencyObject)
        where T : DependencyObject
    {
        if (dependencyObject is null)
        {
            yield break;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
        {
            var child = VisualTreeHelper.GetChild(dependencyObject, i);

            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var childOfChild in FindVisualChildren<T>(child))
            {
                yield return childOfChild;
            }
        }
    }
	
	private void Window_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!UserDropdownPopup.IsOpen)
		{
			return;
		}

		var clickedElement = e.OriginalSource as DependencyObject;

		if (IsDescendantOf(clickedElement, UserSearchTextBox) ||
			IsDescendantOf(clickedElement, UserListBox))
		{
			return;
		}

		CloseDropdown();
	}
	
	private static bool IsDescendantOf(DependencyObject? child, DependencyObject parent)
	{
		while (child is not null)
		{
			if (ReferenceEquals(child, parent))
			{
				return true;
			}

			child = VisualTreeHelper.GetParent(child);
		}

		return false;
	}
}