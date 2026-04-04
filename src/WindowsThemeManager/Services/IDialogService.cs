using System.Windows;

namespace WindowsThemeManager.Services;

/// <summary>
/// Provides user-friendly dialog notifications.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows an information message to the user.
    /// </summary>
    void ShowInfo(string message, string title = "Information");

    /// <summary>
    /// Shows a warning message to the user.
    /// </summary>
    void ShowWarning(string message, string title = "Warning");

    /// <summary>
    /// Shows an error message to the user.
    /// </summary>
    void ShowError(string message, string title = "Error");

    /// <summary>
    /// Shows a confirmation dialog.
    /// </summary>
    bool ShowConfirmation(string message, string title = "Confirm");
}
