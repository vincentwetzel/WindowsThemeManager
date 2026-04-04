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
        MessageBox.Show(_owner, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowWarning(string message, string title = "Warning")
    {
        MessageBox.Show(_owner, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public void ShowError(string message, string title = "Error")
    {
        MessageBox.Show(_owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public bool ShowConfirmation(string message, string title = "Confirm")
    {
        return MessageBox.Show(_owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
            == MessageBoxResult.Yes;
    }
}
