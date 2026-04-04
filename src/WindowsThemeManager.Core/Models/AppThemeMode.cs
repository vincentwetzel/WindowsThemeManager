namespace WindowsThemeManager.Core.Models;

/// <summary>
/// Application theme mode.
/// </summary>
public enum AppThemeMode
{
    /// <summary>
    /// Follow the Windows system theme (light or dark).
    /// </summary>
    System,

    /// <summary>
    /// Always use light theme.
    /// </summary>
    Light,

    /// <summary>
    /// Always use dark theme.
    /// </summary>
    Dark,
}
