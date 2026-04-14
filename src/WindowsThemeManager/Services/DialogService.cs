using System.Windows;

namespace WindowsThemeManager.Services;

/// <summary>
/// WPF-based dialog service for user-friendly messages.
/// </summary>
public class DialogService : IDialogService
{
    private Window? _owner;

    /// <summary>
    /// Sets the owner window for dialogs.
    /// </summary>
    public void SetOwner(Window? owner)
    {
        _owner = owner;
    }

    public void ShowInfo(string message, string title = "Information")
    {
        ShowMessage(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowWarning(string message, string title = "Warning")
    {
        ShowMessage(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public void ShowError(string message, string title = "Error")
    {
        ShowMessage(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public bool ShowConfirmation(string message, string title = "Confirm")
    {
        return ShowMessage(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
            == MessageBoxResult.Yes;
    }

    private MessageBoxResult ShowMessage(
        string message,
        string title,
        MessageBoxButton button,
        MessageBoxImage icon)
    {
        return _owner is null
            ? MessageBox.Show(message, title, button, icon)
            : MessageBox.Show(_owner, message, title, button, icon);
    }
}

